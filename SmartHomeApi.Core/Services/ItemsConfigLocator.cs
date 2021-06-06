using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;

namespace SmartHomeApi.Core.Services
{
    public class ItemsConfigLocator : IItemsConfigLocator
    {
        private const int ConfigsLoadingDelayMsDefault = 500;

        private bool _disposed;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private string _configsDirectory;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        private readonly FileSystemWatcher _watcher;
        private readonly List<string> _configExtensions = new List<string> { ".json" };

        //Key = ItemId, Value = ConfigContainer
        private readonly ConcurrentDictionary<string, ConfigContainer> _configs = new ConcurrentDictionary<string, ConfigContainer>();
        //Key = ConfigPath, Value = ItemId
        private readonly ConcurrentDictionary<string, string> _itemIdByFiles = new ConcurrentDictionary<string, string>();
        //Key = ItemType, Value = (Dictionary where Key = ItemId, Value = ConfigContainer)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConfigContainer>> _configsByItemType =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ConfigContainer>>();

        private readonly ConcurrentDictionary<string, ConfigProcessed> _processedFiles =
            new ConcurrentDictionary<string, ConfigProcessed>();

        public bool IsInitialized { get; private set; }

        public ItemsConfigLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();

            var config = fabric.GetConfiguration();

            EnsureDirectories(config);

            _fabric.GetItemsPluginsLocator().ItemLocatorAddedOrUpdated += OnItemLocatorAddedOrUpdated;
            _fabric.GetItemsPluginsLocator().BeforeItemLocatorDeleted += OnBeforeItemLocatorDeleted;

            _watcher = new FileSystemWatcher(_configsDirectory);
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size;

            foreach (var configExtension in _configExtensions)
            {
                _watcher.Filters.Add("*" + configExtension);
            }
            _watcher.Created += WatcherOnCreated;
            _watcher.Changed += WatcherOnChanged;
            _watcher.Deleted += WatcherOnDeleted;

            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
        }

        public async Task Initialize()
        {
            //Read initial configs here
            var configs = FindConfigs();

            foreach (var config in configs)
            {
                await ProcessConfigFile(config);
            }

            IsInitialized = true;
            _taskCompletionSource.SetResult(true);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _watcher?.Dispose();
                _fabric.GetItemsPluginsLocator().ItemLocatorAddedOrUpdated -= OnItemLocatorAddedOrUpdated;
                _fabric.GetItemsPluginsLocator().ItemLocatorDeleted -= OnBeforeItemLocatorDeleted;

                _disposed = true;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task<List<IItemConfig>> GetItemsConfigs(string itemType)
        {
            await _taskCompletionSource.Task;

            return _configsByItemType.TryGetValue(itemType, out var configContainers)
                ? configContainers.ToList().Where(c => c.Value.ItemConfig != null)
                                  .Select(c => c.Value.ItemConfig).ToList()
                : new List<IItemConfig>();
        }

        private void EnsureDirectories(AppSettings config)
        {
            _configsDirectory = Path.Combine(config.DataDirectoryPath, "Configs");

            if (!Directory.Exists(_configsDirectory))
                Directory.CreateDirectory(_configsDirectory);
        }

        private async void OnItemLocatorAddedOrUpdated(object sender, ItemLocatorEventArgs e)
        {
            var configs = _configsByItemType.TryGetValue(e.ItemType, out var configContainers)
                ? configContainers.ToList().Select(c => c.Value).ToList()
                : new List<ConfigContainer>();

            foreach (var configContainer in configs)
            {
                IItemConfig config = await GetItemConfig(configContainer.ItemType, configContainer.ItemId);

                if (config == null)
                    continue;

                var builder = new ConfigurationBuilder().AddJsonFile(configContainer.ConfigFilePath, optional: true);
                var configRoot = builder.Build();

                configRoot.Bind(config);

                configContainer.ItemConfig = config;

                await OnConfigAdded(configContainer);
            }
        }

        private void OnBeforeItemLocatorDeleted(object sender, ItemLocatorEventArgs e)
        {
            var configs = _configsByItemType.TryGetValue(e.ItemType, out var configContainers)
                ? configContainers.ToList().Select(c => c.Value).ToList()
                : new List<ConfigContainer>();

            //Need to delete all ItemConfig instances in order not to have references to dll with unloaded ItemsLocator
            foreach (var configContainer in configs)
            {
                configContainer.ItemConfig = null;
            }
        }

        private void WatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            ProcessWatcherEvent(e, EventType.Created);
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            ProcessWatcherEvent(e, EventType.Changed);
        }

        private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            ProcessWatcherEvent(e, EventType.Deleted);
        }

        private void ProcessWatcherEvent(FileSystemEventArgs e, EventType eventType)
        {
            try
            {
                var configPath = e.FullPath;

                if (_processedFiles.ContainsKey(configPath))
                    return; //It means this plugin is being processed now.

                //This is needed because actually FileWatcher raises several events on one file change and thus
                //all files in different threads passed previous check. isNewProcessedConfig variable allows to make sure that
                //ProcessConfigWithDelay will be called only once.
                bool isNewProcessedConfig = true;

                _processedFiles.AddOrUpdate(configPath,
                    new ConfigProcessed { ConfigPath = configPath, EventType = eventType }, (key, dir) =>
                    {
                        isNewProcessedConfig = false;
                        return dir;
                    });

                if (isNewProcessedConfig)
                    Task.Run(async () => await ProcessConfigWithDelay(configPath)).ContinueWith(t =>
                        {
                            _processedFiles.TryRemove(configPath, out _);
                            _logger.Error(t.Exception);
                        },
                        TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
        }

        private async Task ProcessConfigWithDelay(string configPath)
        {
            var configsLoadingDelayMs = GetConfigsLoadingDelayMs();

            _logger.Info($"Config {configPath} will be processed in {configsLoadingDelayMs} ms.");

            await Task.Delay(GetConfigsLoadingDelayMs());

            await ProcessConfigFile(configPath);
        }

        private int GetConfigsLoadingDelayMs()
        {
            var configsLoadingDelayMs = _fabric.GetConfiguration().ItemsConfigsLocator.ConfigsLoadingDelayMs;

            return configsLoadingDelayMs == 0 ? ConfigsLoadingDelayMsDefault : configsLoadingDelayMs;
        }

        private List<string> FindConfigs()
        {
            var files = Directory.EnumerateFiles(_configsDirectory, "*.*", SearchOption.AllDirectories)
                                 .Where(s => _configExtensions.Contains(Path.GetExtension(s).ToLowerInvariant())).ToList();

            return files;
        }

        private async Task ProcessConfigFile(string configPath)
        {
            _processedFiles.TryRemove(configPath, out _);

            if (!File.Exists(configPath))
            {
                await ProcessDeletedConfigFile(configPath);
                return;
            }

            var configContent = await File.ReadAllTextAsync(configPath);

            var builder = new ConfigurationBuilder().AddJsonFile(configPath, optional: true);

            IConfigurationRoot configRoot;

            try
            {
                configRoot = builder.Build();
            }
            catch (Exception e)
            {
                _logger.Error(e);

                //Return even if we already had initial valid version of config, i.e. new invalid version will not
                //cause deleting of item.
                _logger.Error($"Config {configPath} has not been processed. Check if json is valid.");
                return;
            }

            string itemType = configRoot.GetValue<string>("ItemType", null);
            string itemId = configRoot.GetValue<string>("ItemId", null);

            if (string.IsNullOrWhiteSpace(itemType))
            {
                _logger.Error($"Missing ItemType parameter in {configPath} config so can't process.");
                return;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                _logger.Error($"Missing ItemId parameter in {configPath} config so can't process.");
                return;
            }

            if (ItemsAreDifferent(configPath, itemType, itemId))
            {
                //Delete previous item and then process new one.
                await ProcessDeletedConfigFile(configPath);
            }

            var container = GetOrCreateConfigContainer(itemId);
            var itemTypeConfigs = _configsByItemType.GetOrAdd(itemType, new ConcurrentDictionary<string, ConfigContainer>());
            itemTypeConfigs.AddOrUpdate(itemId, container, (s, configContainer) => container);

            if (IsConfigDuplicate(container, configPath))
            {
                if (!container.DuplicatesFilePaths.Contains(configPath))
                {
                    container.DuplicatesFilePaths.Add(configPath);
                }

                return;
            }

            bool isAddedConfig = container.ConfigFilePath == null;

            var previousConfigContent = container.ConfigContent;
            container.ConfigContent = configContent;
            container.ItemType = itemType;
            container.ItemId = itemId;
            container.ConfigFilePath = configPath;
            AddFilePath(configPath, itemId);

            IItemConfig config = await GetItemConfig(itemType, itemId);

            if (config == null)
                return;

            configRoot.Bind(config);

            container.ItemConfig = config;

            if (isAddedConfig)
                await OnConfigAdded(container);
            else
                await OnConfigUpdated(container, previousConfigContent);

            _logger.Info($"Config file {configPath} has been successfully processed.");
        }

        private bool ItemsAreDifferent(string path, string itemType, string itemId)
        {
            if (!_itemIdByFiles.TryGetValue(path, out var previousItemId))
                return false; //It means we have not processed this file yet.

            if (previousItemId != itemId)
                return true; //It means previously this file contained another ItemId

            if (!_configs.TryGetValue(itemId, out var container))
            {
                //This case should not be possible but if it happened then log and try to process process as it's first time.
                return false;
            }

            if (container.ItemType != itemType)
                return true; //It means previously this file contained another ItemType

            return false;
        }

        private void AddFilePath(string path, string itemId)
        {
            if (_itemIdByFiles.ContainsKey(path))
                return;

            if (!_itemIdByFiles.TryAdd(path, itemId))
                _logger.Warning($"File {path} for ItemId {itemId} has not been added to _itemIdByFiles dictionary.");
        }

        private bool TryRemoveFilePath(string path, out string itemId)
        {
            if (!_itemIdByFiles.TryRemove(path, out itemId))
            {
                _logger.Error($"File {path} has not been removed from _itemIdByFiles dictionary.");
                return false;
            }

            return true;
        }

        private async Task ProcessDeletedConfigFile(string configPath)
        {
            //Try to remove file from dictionary and get ItemId, if it's impossible then further processing is also impossible.
            if (!TryRemoveFilePath(configPath, out var itemId))
                return;

            if (!_configs.TryRemove(itemId, out var configContainer))
            {
                _logger.Error($"{itemId} ConfigContainer has not been found in _configs dictionary.");
                return;
            }

            if (_configsByItemType.TryGetValue(configContainer.ItemType, out var itemTypeConfigs))
            {
                if (!itemTypeConfigs.TryRemove(itemId, out _))
                {
                    _logger.Error($"{itemId} ConfigContainer has not been found in _configsByItemType dictionary.");
                }
            }

            await OnConfigDeleted(configContainer);
        }

        private ConfigContainer GetOrCreateConfigContainer(string itemId)
        {
            return _configs.GetOrAdd(itemId, s => new ConfigContainer());
        }

        private bool IsConfigDuplicate(ConfigContainer container, string configFilePath)
        {
            //It means config for this Item has not been processed yet
            if (string.IsNullOrEmpty(container.ConfigFilePath))
                return false;

            //It means config for this Item has been already processed and it's the same file
            if (container.ConfigFilePath == configFilePath)
                return false;

            _logger.Warning($"Config file [{configFilePath}] has been ignored " +
                            $"because it is duplicate of already processed config file [{container.ConfigFilePath}].");

            return true;
        }

        private async Task<IItemConfig> GetItemConfig(string itemType, string itemId)
        {
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();

            var locator = locators.FirstOrDefault(l => l.ItemType == itemType);

            if (locator == null)
                return null;

            var config = (IItemConfig)Activator.CreateInstance(locator.ConfigType, itemId, itemType);

            return config;
        }

        private async Task OnConfigAdded(ConfigContainer container)
        {
            var bridge = await GetBridge(container);

            if (bridge == null || !ItemsLocatorIsInitialized(bridge))
                return;

            _ = Task.Run(async () => await bridge.ConfigAdded(container.ItemConfig))
                    .ContinueWith(t => { _logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task OnConfigUpdated(ConfigContainer container, string previousConfigContent)
        {
            var bridge = await GetBridge(container);

            if (bridge == null || !ItemsLocatorIsInitialized(bridge))
                return;

            if (!string.IsNullOrEmpty(previousConfigContent))
            {
                //Check if config content was changed. If not then no need to emit event.
                if (previousConfigContent == container.ConfigContent)
                    return;
            }

            _ = Task.Run(async () => await bridge.ConfigUpdated(container.ItemConfig))
                    .ContinueWith(t => { _logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task OnConfigDeleted(ConfigContainer container)
        {
            var bridge = await GetBridge(container);

            if (bridge == null || !ItemsLocatorIsInitialized(bridge))
                return;

            _ = Task.Run(async () => await bridge.ConfigDeleted(container.ItemConfig.ItemId))
                    .ContinueWith(t => { _logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task<IStandardItemsLocatorBridge> GetBridge(ConfigContainer container)
        {
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();
            var locator = locators.FirstOrDefault(l => l.ItemType == container.ItemConfig.ItemType);

            if (locator == null)
            {
                _logger.Error($"Config of {container.ItemConfig.ItemType} type has been processed but ItemsLocator " +
                              "of this type does not exist.");
                return null;
            }

            return locator;
        }

        private bool ItemsLocatorIsInitialized(IStandardItemsLocatorBridge bridge)
        {
            if (!bridge.IsInitialized)
            {
                _logger.Warning($"ItemsLocator of {bridge.ItemType} has not been initialized yet so can't notify it about config changes.");
                return false;
            }

            return true;
        }

        private class ConfigContainer
        {
            public string ConfigFilePath { get; set; }
            public IList<string> DuplicatesFilePaths { get; set; } = new List<string>();
            public IItemConfig ItemConfig { get; set; }
            public string ItemId { get; set; }
            public string ItemType { get; set; }
            public string ConfigContent { get; set; }
        }

        private class ConfigProcessed
        {
            public string ConfigPath { get; set; }
            public EventType EventType { get; set; }
        }

        private enum EventType
        {
            Created,
            Changed,
            Deleted
        }
    }
}