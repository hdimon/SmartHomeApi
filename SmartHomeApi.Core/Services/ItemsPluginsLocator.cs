using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Interfaces.ItemsLocators;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;
using SmartHomeApi.Core.ItemsLocatorsBridges;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator : IItemsPluginsLocator
    {
        private const int PluginsUnloadingAttemptsIntervalMSDefault = 1000;
        private const int ItemLocatorConstructorTimeoutMSDefault = 5000;
        private const int UnloadPluginsMaxTriesDefault = 5;
        private const int UnloadPluginsTriesIntervalMSDefault = 500;

        private bool _disposed;
        private string _pluginsDirectory;
        private string _tempPluginsDirectory;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly IPluginsFileWatcher _fileWatcher;
        private readonly List<string> _librariesExtensions = new List<string> { ".dll" };
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        private readonly ConcurrentDictionary<string, PluginContainer> _pluginContainers =
            new ConcurrentDictionary<string, PluginContainer>();

        private readonly ConcurrentDictionary<string, IStandardItemsLocatorBridge>
            _locators = new ConcurrentDictionary<string, IStandardItemsLocatorBridge>();

        public event EventHandler<ItemLocatorEventArgs> ItemLocatorAddedOrUpdated;
        public event EventHandler<ItemLocatorEventArgs> BeforeItemLocatorDeleted;
        public event EventHandler<ItemLocatorEventArgs> ItemLocatorDeleted;

        public bool IsInitialized { get; private set; }

        public ItemsPluginsLocator(ISmartHomeApiFabric fabric)
        {
            var unused = typeof(AverageValuesHelper); //Workaround to load dll

            _semaphoreSlim = new SemaphoreSlim(1, 1);
            var config = fabric.GetConfiguration();

            EnsureDirectories(config);

            _fabric = fabric;
            _logger = fabric.GetApiLogger();

            _fileWatcher = new PluginsFileWatcher(fabric, _pluginsDirectory, _librariesExtensions);
            _fileWatcher.PluginAddedOrUpdated += FileWatcherOnPluginAddedOrUpdated;
            _fileWatcher.PluginDeleted += FileWatcherOnPluginDeleted;
        }

        public async Task Initialize()
        {
            var initialPlugins = _fileWatcher.FindPlugins();

            await Task.WhenAll(initialPlugins.Select(ProcessPluginAddedOrUpdatedEvent));

            IsInitialized = true;
        }

        public async Task<IEnumerable<IStandardItemsLocatorBridge>> GetItemsLocators()
        {
            return _locators.Values;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _fileWatcher?.Dispose();

            _disposingCancellationTokenSource.Cancel();

            _logger.Info("Disposing ItemsPluginsLocator...");

            DeletePlugins(_pluginContainers.Values.ToList()).Wait();

            _logger.Info("ItemsPluginsLocator has been disposed.");

            _disposed = true;
        }

        private void EnsureDirectories(AppSettings config)
        {
            _pluginsDirectory = Path.Combine(config.DataDirectoryPath, "Plugins");
            _tempPluginsDirectory = Path.Combine(config.DataDirectoryPath, "TempPlugins");

            if (!Directory.Exists(_pluginsDirectory))
                Directory.CreateDirectory(_pluginsDirectory);

            if (Directory.Exists(_tempPluginsDirectory))
                Directory.Delete(_tempPluginsDirectory, true);

            Directory.CreateDirectory(_tempPluginsDirectory);
        }

        private void OnItemLocatorAddedOrUpdated(ItemLocatorEventArgs e)
        {
            Task.Run(() => ItemLocatorAddedOrUpdated?.Invoke(this, e)).ContinueWith(t => { _logger.Error(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private void OnItemLocatorDeleted(ItemLocatorEventArgs e)
        {
            Task.Run(() => ItemLocatorDeleted?.Invoke(this, e)).ContinueWith(t => { _logger.Error(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private void OnBeforeItemLocatorDeleted(ItemLocatorEventArgs e)
        {
            Task.Run(() => BeforeItemLocatorDeleted?.Invoke(this, e)).ContinueWith(t => { _logger.Error(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private async void FileWatcherOnPluginAddedOrUpdated(object sender, PluginEventArgs e)
        {
            await ProcessPluginAddedOrUpdatedEvent(e);
        }

        private async void FileWatcherOnPluginDeleted(object sender, PluginEventArgs e)
        {
            var pluginContainer = new PluginContainer();
            pluginContainer.PluginDirectoryName = e.PluginDirectoryName;
            pluginContainer.PluginDirectoryInfo = e.PluginDirectoryInfo;

            await ProcessDeletedPlugin(pluginContainer);
        }

        private async Task ProcessPluginAddedOrUpdatedEvent(PluginEventArgs e)
        {
            try
            {
                var pluginContainer = new PluginContainer();
                pluginContainer.PluginDirectoryName = e.PluginDirectoryName;
                pluginContainer.PluginDirectoryInfo = e.PluginDirectoryInfo;
                pluginContainer.DllFiles = e.DllFiles;
                pluginContainer.Files = e.Files;

                await ProcessPlugin(pluginContainer);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task ProcessPlugin(PluginContainer pluginContainer)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (_pluginContainers.ContainsKey(pluginContainer.PluginDirectoryName))
                    await UpdatePlugin(pluginContainer);
                else
                    await AddPlugin(pluginContainer);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task AddPlugin(PluginContainer pluginContainer)
        {
            try
            {
                await LoadPlugin(pluginContainer, null);
            }
            catch (Exception e)
            {
                _logger.Error(e);

                DeleteTempPlugin(pluginContainer);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task LoadPlugin(PluginContainer plugin, List<DeletedItemsLocator> deletedItemsLocators)
        {
            var tempPlugin = CopyPluginToTempDirectory(plugin);
            List<IItemsLocator> locators = new List<IItemsLocator>();

            foreach (var dll in tempPlugin.TempDllFiles)
            {
                var dllPath = dll.FullName;

                var checkPluginResult = LoadDllIfPlugin(dllPath);

                if (!checkPluginResult.Success)
                {
                    if (checkPluginResult.Reference != null)
                        CollectGarbage(checkPluginResult.Reference, dllPath);
                    continue;
                }

                var context = checkPluginResult.AssemblyContext;
                var locatorTypes = checkPluginResult.LocatorTypes;

                foreach (var type in locatorTypes)
                {
                    IItemsLocator instance = null;
                    var itemLocatorConstructorTimeout = GetItemLocatorConstructorTimeoutMS();
                    var cts = new CancellationTokenSource(itemLocatorConstructorTimeout);

                    try
                    {
                        var task = Task.Run(() => (IItemsLocator)Activator.CreateInstance(type, _fabric), cts.Token);
                        var completedTask = await Task.WhenAny(task, Task.Delay(itemLocatorConstructorTimeout, cts.Token));

                        if (completedTask == task)
                            instance = await task;
                        else
                            throw new TaskCanceledException();
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.Info(
                            $"ItemLocator's constructor (type {type}) has exceeded timeout in {itemLocatorConstructorTimeout} ms.");
                    }

                    if (instance == null)
                    {
                        _logger.Info($"ItemLocator of type {type} has not been created");
                        continue;
                    }

                    if (_locators.ContainsKey(instance.ItemType))
                    {
                        _logger.Warning($"ItemLocator {instance.ItemType} has been ignored because " +
                                        "locator with the same type already exists.");
                        continue;
                    }

                    var bridge = GetItemsLocatorBridge(instance);

                    _locators.TryAdd(instance.ItemType, bridge);
                    locators.Add(bridge);

                    _logger.Info($"ItemLocator {instance.ItemType} has been created");
                }

                tempPlugin.Locators = locators;
                tempPlugin.AssemblyContext = context;

                //We support only one dll with plugins in directory so just stop processing
                break;
            }

            if (deletedItemsLocators != null)
            {
                foreach (var deletedItemsLocator in deletedItemsLocators)
                {
                    if (locators.All(l => l.ItemType != deletedItemsLocator.ItemType))
                        OnItemLocatorDeleted(new ItemLocatorEventArgs { ItemType = deletedItemsLocator.ItemType });
                }
            }

            foreach (var itemsLocator in locators)
            {
                OnItemLocatorAddedOrUpdated(new ItemLocatorEventArgs { ItemType = itemsLocator.ItemType });
            }

            _pluginContainers.TryAdd(tempPlugin.PluginDirectoryName, tempPlugin);

            _logger.Info($"Plugin {tempPlugin.PluginDirectoryName} has been processed");
        }

        private IStandardItemsLocatorBridge GetItemsLocatorBridge(IItemsLocator locator)
        {
            if (locator is IStandardItemsLocator standard)
                return new StandardItemsLocatorBridge(standard);

            return new DeprecatedItemsLocatorBridge(locator);
        }

        private async Task UpdatePlugin(PluginContainer pluginContainer)
        {
            if (!_pluginContainers.ContainsKey(pluginContainer.PluginDirectoryName))
            {
                _logger.Error(
                    $"Plugin {pluginContainer.PluginDirectoryName} is being updated but it's not in existing plugins list.");
                return;
            }

            var existingPlugin = _pluginContainers[pluginContainer.PluginDirectoryName];
            existingPlugin.Files = pluginContainer.Files;
            existingPlugin.DllFiles = pluginContainer.DllFiles;

            if (!await PluginWasChanged(existingPlugin))
                return;

            try
            {
                _logger.Info($"Plugin {existingPlugin.PluginDirectoryName} was changed, try to reload it...");

                var deletedPlugin = await DeletePlugin(existingPlugin);

                if (deletedPlugin != null)
                    EmitOnBeforeItemLocatorDeletedEvent(deletedPlugin.Plugin.Locators);

                var deletedItemsLocators = await UnloadPlugins(new List<DeletingPluginContainer> { deletedPlugin });
                await LoadPlugin(existingPlugin, deletedItemsLocators);

                _logger.Info($"Plugin {existingPlugin.PluginDirectoryName} successfully reloaded.");
            }
            catch (Exception e)
            {
                DeleteTempPlugin(existingPlugin);

                _logger.Error(e);
            }
        }

        private void EmitOnBeforeItemLocatorDeletedEvent(List<IItemsLocator> locators)
        {
            foreach (var locator in locators)
            {
                var eventArgs = new ItemLocatorEventArgs { ItemType = locator.ItemType };
                OnBeforeItemLocatorDeleted(eventArgs);
            }
        }

        private PluginContainer CopyPluginToTempDirectory(PluginContainer plugin)
        {
            var tempPluginDirectoryPath = GetPluginPath(plugin);

            var tempFiles = new List<FileInfo>();

            Directory.CreateDirectory(tempPluginDirectoryPath);

            foreach (var file in plugin.Files)
            {
                var relativePath = Path.GetRelativePath(plugin.PluginDirectoryInfo.FullName, file.DirectoryName);

                var tempDirectoryPath = Path.Combine(tempPluginDirectoryPath, relativePath);
                Directory.CreateDirectory(tempDirectoryPath);
                var tempFilePath = Path.Combine(tempPluginDirectoryPath, relativePath, file.Name);

                try
                {
                    File.Copy(file.FullName, tempFilePath, true);
                }
                catch (Exception)
                {
                    if (!IsSoftPluginsLoading())
                        throw;
                }

                tempFiles.Add(new FileInfo(tempFilePath));
            }

            plugin.TempFiles = tempFiles;
            plugin.TempDllFiles = tempFiles
                                  .Where(f => _librariesExtensions.Contains(
                                      Path.GetExtension(f.Name).ToLowerInvariant())).ToList();

            return plugin;
        }

        private ItemsPluginsLocatorSettings GetSettings()
        {
            return _fabric.GetConfiguration().ItemsPluginsLocator;
        }

        private bool IsSoftPluginsLoading()
        {
            return GetSettings().SoftPluginsLoading;
        }

        private void DeleteTempPlugin(PluginContainer plugin)
        {
            try
            {
                var tempPluginDirectoryPath = GetPluginPath(plugin);

                if (Directory.Exists(tempPluginDirectoryPath))
                    Directory.Delete(tempPluginDirectoryPath, true);
            }
            catch (Exception e)
            {
                _logger.Error($"Error during temp directory deleting: {e}");
            }
        }

        private string GetPluginPath(PluginContainer plugin)
        {
            return Path.Combine(_tempPluginsDirectory, plugin.PluginDirectoryName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private LoadingPluginResult LoadDllIfPlugin(string dllPath)
        {
            var context = new CollectibleAssemblyContext(dllPath);

            Assembly assembly = null;

            //Load from FileStream instead of by AssemblyName because it never blocks dll.
            using (var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    assembly = context.LoadFromStream(fs);
                }
                catch (BadImageFormatException)
                {
                    //_logger.Error(e);
                }
            }

            if (assembly == null)
                return new LoadingPluginResult { Success = false, Reference = null };
            /*Assembly assembly =
                context.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(dllPath)));*/

            var locatorType = typeof(IItemsLocator);

            var locatorTypes = assembly.GetTypes().Where(p => locatorType.IsAssignableFrom(p)).ToList();

            if (locatorTypes.Any())
                return new LoadingPluginResult
                {
                    Success = true, 
                    AssemblyContext = context, 
                    Assembly = assembly,
                    LocatorTypes = locatorTypes
                };

            context.Unload();

            var weakReference = new WeakReference(context);

            return new LoadingPluginResult { Success = false, Reference = weakReference };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool CollectGarbage(WeakReference weakReference, string dll)
        {
            try
            {
                for (int i = 0; i < 8 && weakReference.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                if (weakReference.IsAlive)
                {
                    _logger.Error($"Unloading {dll} failed");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return false;
        }

        private async Task<bool> PluginWasChanged(PluginContainer plugin)
        {
            var existingFiles = plugin.DllFiles;
            var newFiles = plugin.TempDllFiles;

            if (existingFiles.Count != newFiles.Count)
                return true;

            var newFilesByNames = newFiles.ToDictionary(f => f.Name, f => f);

            foreach (var existingFile in existingFiles)
            {
                if (!newFilesByNames.ContainsKey(existingFile.Name))
                    return true;

                var newFile = newFilesByNames[existingFile.Name];

                if (!newFile.Exists || existingFile.Length != newFile.Length)
                    return true;

                var existingBytes = await File.ReadAllBytesAsync(existingFile.FullName);
                var newBytes = await File.ReadAllBytesAsync(newFile.FullName);

                for (long i = 0; i < existingBytes.LongLength; i++)
                {
                    if (existingBytes[i] != newBytes[i])
                        return true;
                }
            }

            return false;
        }

        private async Task ProcessDeletedPlugin(PluginContainer pluginContainer)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var deletedPlugin = await DeletePlugin(pluginContainer);
                var deletedItemsLocators = await UnloadPlugins(new List<DeletingPluginContainer> { deletedPlugin });

                foreach (var deletedItemsLocator in deletedItemsLocators)
                {
                    OnItemLocatorDeleted(new ItemLocatorEventArgs { ItemType = deletedItemsLocator.ItemType });
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<List<DeletingPluginContainer>> DeletePlugins(List<PluginContainer> plugins)
        {
            var references = new List<DeletingPluginContainer>();

            if (!plugins.Any())
                return references;

            foreach (var plugin in plugins)
            {
                try
                {
                    var container = await DeletePlugin(plugin);

                    if (container != null)
                        references.Add(container);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }

            return references;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<DeletingPluginContainer> DeletePlugin(PluginContainer plugin)
        {
            if (!_pluginContainers.TryGetValue(plugin.PluginDirectoryName, out var deletedContainer))
                return null;

            _logger.Info($"Plugin {plugin.PluginDirectoryName} has been deleted.");

            foreach (var itemsLocator in deletedContainer.Locators)
            {
                if (itemsLocator is IDisposable disposable)
                {
                    disposable.Dispose();
                    _logger.Info($"ItemsLocator {itemsLocator.ItemType} has been disposed.");
                }
            }

            deletedContainer.AssemblyContext.Unload();

            var weakReference = new WeakReference(deletedContainer.AssemblyContext);

            var container = new DeletingPluginContainer { Reference = weakReference, Plugin = deletedContainer };
            container.Plugin.AssemblyContext = null; //Delete reference to allow GC to make its work

            _logger.Info($"Assemblies from {plugin.PluginDirectoryInfo.FullName} are being unloaded.");

            _pluginContainers.TryRemove(plugin.PluginDirectoryName, out var t);

            return container;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<List<DeletedItemsLocator>> UnloadPlugins(List<DeletingPluginContainer> deleteContainers)
        {
            var deletedLocators = new List<DeletedItemsLocator>();

            if (!deleteContainers.Any())
                return deletedLocators;

            foreach (var deleteContainer in deleteContainers)
            {
                try
                {
                    await AsyncHelpers.RetryOnFault(async () =>
                        {
                            var removingFailed = false;

                            //Remove locator from existing dictionary in order to get rid of references to being deleted objects
                            foreach (var itemsLocator in deleteContainer.Plugin.Locators)
                            {
                                if (!_locators.TryRemove(itemsLocator.ItemType, out _))
                                    removingFailed = true;
                                else
                                    deletedLocators.Add(new DeletedItemsLocator { ItemType = itemsLocator.ItemType });
                            }

                            if (!removingFailed)
                                deleteContainer.Plugin.Locators.Clear();

                            await Task.Delay(GetPluginsUnloadingAttemptsIntervalMs(), _disposingCancellationTokenSource.Token);

                            if (!CollectGarbage(deleteContainer.Reference, deleteContainer.Plugin.PluginDirectoryName))
                                throw new Exception("Could not unload dll. It's recommended to restart service.");

                            try
                            {
                                var tempPluginDirectoryPath = GetPluginPath(deleteContainer.Plugin);

                                await AsyncHelpers.RetryOnFault(async () => Directory.Delete(tempPluginDirectoryPath, true), 5,
                                    () => Task.Delay(GetPluginsUnloadingAttemptsIntervalMs(),
                                        _disposingCancellationTokenSource.Token));
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e);
                            }

                            _logger.Info($"{deleteContainer.Plugin.PluginDirectoryName} has been unloaded.");
                        }, GetUnloadPluginsMaxTries(),
                        () => Task.Delay(GetUnloadPluginsTriesIntervalMS(), _disposingCancellationTokenSource.Token));
                }
                catch (Exception e)
                {
                    if (IsSoftPluginsLoading())
                    {
                        _logger.Info("Could not unload dll but SoftPluginsLoading is True so continue working.");
                        //Try to clean as much as we can
                        try
                        {
                            DeleteTempPlugin(deleteContainer.Plugin);
                        }
                        catch (Exception)
                        {
                            //Since it's SoftLoading then just suppress exception
                            //_logger.Error(exception);
                        }
                    }
                    else
                    {
                        _logger.Error(e);
                    }
                }
            }

            _logger.Info("Assemblies unloading has been finished.");

            return deletedLocators;
        }

        private int GetPluginsUnloadingAttemptsIntervalMs()
        {
            var pluginsUnloadingAttemptsIntervalMs = GetSettings().PluginsUnloadingAttemptsIntervalMs;

            return pluginsUnloadingAttemptsIntervalMs == 0
                ? PluginsUnloadingAttemptsIntervalMSDefault
                : pluginsUnloadingAttemptsIntervalMs;
        }

        private int GetUnloadPluginsMaxTries()
        {
            var unloadPluginsMaxTries = GetSettings().UnloadPluginsMaxTries;

            return unloadPluginsMaxTries > 0 ? unloadPluginsMaxTries : UnloadPluginsMaxTriesDefault;
        }

        private int GetUnloadPluginsTriesIntervalMS()
        {
            var unloadPluginsTriesIntervalMS = GetSettings().UnloadPluginsTriesIntervalMS;

            return unloadPluginsTriesIntervalMS > 0 ? unloadPluginsTriesIntervalMS : UnloadPluginsTriesIntervalMSDefault;
        }

        private int GetItemLocatorConstructorTimeoutMS()
        {
            var itemLocatorConstructorTimeoutMS = GetSettings().ItemLocatorConstructorTimeoutMS;

            return itemLocatorConstructorTimeoutMS > 0 ? itemLocatorConstructorTimeoutMS : ItemLocatorConstructorTimeoutMSDefault;
        }

        private class DeletingPluginContainer
        {
            public WeakReference Reference { get; set; }
            public PluginContainer Plugin { get; set; }
        }

        private class DeletedItemsLocator
        {
            public string ItemType { get; set; }
        }
    }
}