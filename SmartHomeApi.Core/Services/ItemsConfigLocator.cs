using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ItemsConfigLocator : IItemsConfigLocator
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private Task _worker;
        private string _configDirectory;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private volatile bool _isFirstRun = true;

        private Dictionary<string, ConfigContainer> _configContainers = new Dictionary<string, ConfigContainer>();
        private ConcurrentDictionary<string, string> _knownDuplicateItems = new ConcurrentDictionary<string, string>();

        public bool IsInitialized { get; private set; }

        public ItemsConfigLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();
            _configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
        }

        public async Task Initialize()
        {
            RunConfigsCollectorWorker();

            IsInitialized = true;
        }

        private void RunConfigsCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(ConfigsCollectorWorkerWrapper).Unwrap().ContinueWith(
                    t =>
                    {
                        _logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task ConfigsCollectorWorkerWrapper()
        {
            while (true)
            {
                if (!_isFirstRun)
                    await Task.Delay(1000);

                try
                {
                    await ConfigsCollectorWorker();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
        }

        private async Task ConfigsCollectorWorker()
        {
            try
            {
                var configContainers = new Dictionary<string, ConfigContainer>();

                var ext = new List<string> { ".json" };
                var files = Directory.EnumerateFiles(_configDirectory, "*.*", SearchOption.AllDirectories)
                                     .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())).ToList();

                foreach (var file in files)
                {
                    var container = new ConfigContainer();
                    container.FilePath = file;
                    container.Builder = new ConfigurationBuilder().AddJsonFile(file, optional: true);
                    container.Root = container.Builder.Build();

                    configContainers.Add(file, container);
                }

                foreach (var configContainer in configContainers)
                {
                    await SetItemConfig(configContainer.Value);
                }

                var duplicates = configContainers.Where(c => c.Value.ItemConfig != null)
                                                 .GroupBy(c => c.Value.ItemConfig.ItemId)
                                                 .Where(g => g.Count() > 1)
                                                 .Select(g =>
                                                     new KeyValuePair<string, List<string>>(g.Key,
                                                         g.Select(c => c.Key).ToList())).ToList();

                LogDuplicateConfigs(duplicates);

                var duplicateItemIds = duplicates.Select(d => d.Key).ToList();

                _configContainers = new Dictionary<string, ConfigContainer>(configContainers
                                                                            .Where(c => c.Value.ItemConfig != null)
                                                                            .Where(c => !duplicateItemIds.Contains(
                                                                                c.Value.ItemConfig.ItemId)));

                if (_isFirstRun)
                {
                    _taskCompletionSource.SetResult(true);
                    _isFirstRun = false;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private async Task SetItemConfig(ConfigContainer container)
        {
            string itemType = container.Root.GetValue<string>("ItemType", null);
            string itemId = container.Root.GetValue<string>("ItemId", null);

            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(itemId))
                return;

            IItemConfig config = await GetItemConfig(itemType, itemId, container);

            if (config == null)
                return;

            container.Root.Bind(config);
            container.ItemConfig = config;
        }

        private async Task<IItemConfig> GetItemConfig(string itemType, string itemId, ConfigContainer container)
        {
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();

            var locator = locators.FirstOrDefault(l => l.ItemType == itemType);

            if (locator == null) 
                return null;

            var config = container.ItemConfig ??
                         (IItemConfig)Activator.CreateInstance(locator.ConfigType, itemId, itemType);

            return config;
        }

        private void LogDuplicateConfigs(List<KeyValuePair<string, List<string>>> duplicates)
        {
            var notRelevantDuplicates = _knownDuplicateItems.Keys.Except(duplicates.Select(d => d.Key)).ToList();

            foreach (var notRelevantDuplicate in notRelevantDuplicates)
            {
                _knownDuplicateItems.TryRemove(notRelevantDuplicate, out var test);
            }

            foreach (var duplicate in duplicates)
            {
                if (!_knownDuplicateItems.ContainsKey(duplicate.Key))
                {
                    _logger.Error(
                        $"Next config files have the same ItemId [{duplicate.Key}] so were ignored: {string.Join(", ", duplicate.Value)}.");

                    _knownDuplicateItems.TryAdd(duplicate.Key, duplicate.Key);
                }
            }
        }

        public async Task<List<IItemConfig>> GetItemsConfigs(string itemType)
        {
            if (_isFirstRun)
                await _taskCompletionSource.Task;

            var configs = _configContainers.Values.Select(c => c.ItemConfig).Where(c => c != null).ToList();

            if (configs.Any(c => c.ItemType == itemType))
                return configs.Where(c => c.ItemType == itemType).ToList();

            return new List<IItemConfig>();
        }

        private class ConfigContainer
        {
            public string FilePath { get; set; }
            public IConfigurationBuilder Builder { get; set; }
            public IConfigurationRoot Root { get; set; }
            public IItemConfig ItemConfig { get; set; }
        }
    }
}