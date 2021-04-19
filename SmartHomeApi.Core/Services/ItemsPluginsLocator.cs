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
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator : IItemsPluginsLocator
    {
        private readonly string _pluginsDirectory;
        private readonly string _tempPluginsDirectory;
        private Task _worker;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private volatile bool _isFirstRun = true;
        private readonly List<string> _librariesExtensions = new List<string> { ".dll" };
        private readonly bool _softPluginsLoading = true;
        private readonly int _unloadPluginsMaxTries = 5;
        private readonly int _unloadPluginsTriesIntervalMS = 500;
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        private ConcurrentDictionary<string, IItemsLocator> _locators = new ConcurrentDictionary<string, IItemsLocator>();

        private readonly ConcurrentDictionary<string, PluginContainer> _knownPluginContainers =
            new ConcurrentDictionary<string, PluginContainer>();

        public event EventHandler<ItemLocatorEventArgs> ItemLocatorAddedOrUpdated;
        public event EventHandler<ItemLocatorEventArgs> ItemLocatorDeleted;

        public bool IsInitialized { get; private set; }

        public ItemsPluginsLocator(ISmartHomeApiFabric fabric)
        {
            var config = fabric.GetConfiguration();

            _pluginsDirectory = Path.Combine(config.DataDirectoryPath, "Plugins");
            _tempPluginsDirectory = Path.Combine(config.DataDirectoryPath, "TempPlugins");

            if (Directory.Exists(_tempPluginsDirectory))
                Directory.Delete(_tempPluginsDirectory, true);

            Directory.CreateDirectory(_tempPluginsDirectory);

            _softPluginsLoading = config.ItemsPluginsLocator.SoftPluginsLoading;
            _unloadPluginsMaxTries =
                config.ItemsPluginsLocator.UnloadPluginsMaxTries > 0 ? config.ItemsPluginsLocator.UnloadPluginsMaxTries : _unloadPluginsMaxTries;
            _unloadPluginsTriesIntervalMS = config.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS > 0
                ? config.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS
                : _unloadPluginsTriesIntervalMS;
            //_logger.Info($"SoftPluginsLoading = {config.SoftPluginsLoading}");

            _fabric = fabric;
            _logger = fabric.GetApiLogger();

            RunPluginsCollectorWorker();

            var unused = typeof(AverageValuesHelper); //Workaround to load dll
        }

        public async Task Initialize()
        {
            IsInitialized = true;
        }

        private void RunPluginsCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(PluginsCollectorWorkerWrapper).Unwrap().ContinueWith(
                    t =>
                    {
                        _logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task PluginsCollectorWorkerWrapper()
        {
            while (!_disposingCancellationTokenSource.IsCancellationRequested)
            {
                if (!_isFirstRun)
                    await Task.Delay(GetWorkerInterval(), _disposingCancellationTokenSource.Token);

                try
                {
                    await PluginsCollectorWorker();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
        }

        private int GetWorkerInterval()
        {
            //return _fabric.GetConfiguration().PluginsLocatorIntervalMs ?? 1000;
            return 30000;
        }

        private async Task PluginsCollectorWorker()
        {
            ConcurrentDictionary<string, IItemsLocator> locators = new ConcurrentDictionary<string, IItemsLocator>();

            await ProcessPlugins(locators);

            //Make sure that ref is not cached
            Interlocked.Exchange(ref _locators, locators);

            if (_isFirstRun)
            {
                _taskCompletionSource.SetResult(true);
                _isFirstRun = false;
            }
        }

        private async Task ProcessPlugins(ConcurrentDictionary<string, IItemsLocator> locators)
        {
            var plugins = await CollectPluginContainers(_pluginsDirectory);
            var existingPlugins = await CollectPluginContainers(_tempPluginsDirectory);

            var deletedPlugins = FindDeletedPlugins(plugins, existingPlugins);
            var addedPlugins = FindAddedPlugins(plugins, existingPlugins);
            var updatedPlugins = FindUpdatedPlugins(plugins, existingPlugins);

            await UpdatePlugins(updatedPlugins, locators);
            await AddPlugins(addedPlugins, locators);

            var deleteContainers = await DeletePlugins(deletedPlugins);

            await UnloadPlugins(deleteContainers);
        }

        private List<PluginContainer> FindDeletedPlugins(List<PluginContainer> plugins,
            List<PluginContainer> existingPlugins)
        {
            var deletedPluginNames = existingPlugins
                                     .Select(c => c.PluginName)
                                     .Except(plugins.Select(c => c.PluginName)).ToList();

            var deletedPlugins = existingPlugins.Where(p => deletedPluginNames.Contains(p.PluginName)).ToList();

            return deletedPlugins;
        }

        private List<PluginContainer> FindAddedPlugins(List<PluginContainer> plugins,
            List<PluginContainer> existingPlugins)
        {
            var addedPluginNames = plugins.Select(p => p.PluginName).Except(_knownPluginContainers.Keys).ToList();

            var addedPlugins = plugins.Where(p => addedPluginNames.Contains(p.PluginName)).ToList();

            //Even if there are plugins which were unloaded but not physically deleted from disk then anyway try to load them
            if (_softPluginsLoading)
                return addedPlugins;

            //If some plugins were unloaded but not (fully) deleted from disk then don't take them
            var added = addedPluginNames.Except(existingPlugins.Select(p => p.PluginName)).ToList();

            addedPlugins = plugins.Where(p => added.Contains(p.PluginName)).ToList();

            return addedPlugins;
        }

        private List<PluginContainer> FindUpdatedPlugins(List<PluginContainer> plugins,
            List<PluginContainer> existingPlugins)
        {
            //Take only plugins which are known
            var updatedPluginNames = plugins.Select(p => p.PluginName).Intersect(_knownPluginContainers.Keys).ToList();

            var updatedPlugins = plugins.Where(p => updatedPluginNames.Contains(p.PluginName)).ToList();

            return updatedPlugins;
        }

        private async Task<List<PluginContainer>> CollectPluginContainers(string directory)
        {
            var pluginContainers = new List<PluginContainer>();

            if (!Directory.Exists(directory))
                return pluginContainers;

            var pluginDirectories = Directory.EnumerateDirectories(directory).ToList();

            foreach (var pluginDirectoryPath in pluginDirectories)
            {
                var files = Directory.EnumerateFiles(pluginDirectoryPath, "*.*", SearchOption.AllDirectories)
                                     .Select(f => new FileInfo(f)).ToList();

                if (!files.Any())
                    continue;

                var directoryInfo = new DirectoryInfo(pluginDirectoryPath);

                var pluginContainer = new PluginContainer();
                pluginContainer.PluginName = directoryInfo.Name;
                pluginContainer.PluginDirectoryInfo = directoryInfo;
                pluginContainer.Files = files;
                pluginContainer.DllFiles =
                    files.Where(f => _librariesExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()))
                         .ToList();

                pluginContainers.Add(pluginContainer);
            }

            return pluginContainers;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task AddPlugins(List<PluginContainer> plugins, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            foreach (var pluginContainer in plugins)
            {
                try
                {
                    await LoadPlugin(pluginContainer, locators);
                }
                catch (Exception e)
                {
                    DeleteTempPlugin(pluginContainer);

                    _logger.Error(e);
                }
            }
        }

        private void DeleteTempPlugin(PluginContainer plugin)
        {
            var tempPluginDirectoryPath = GetPluginPath(plugin);

            if (Directory.Exists(tempPluginDirectoryPath))
                Directory.Delete(tempPluginDirectoryPath, true);
        }

        private string GetPluginPath(PluginContainer plugin)
        {
            return Path.Combine(_tempPluginsDirectory, plugin.PluginName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task UpdatePlugins(List<PluginContainer> plugins, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            foreach (var plugin in plugins)
            {
                if (!_knownPluginContainers.ContainsKey(plugin.PluginName))
                    continue;

                var existingPlugin = _knownPluginContainers[plugin.PluginName];
                existingPlugin.Files = plugin.Files;
                existingPlugin.DllFiles = plugin.DllFiles;

                if (!await PluginWasChanged(existingPlugin))
                {
                    foreach (var itemsLocator in existingPlugin.Locators)
                    {
                        locators.TryAdd(itemsLocator.ItemType, itemsLocator);
                    }
                }
                else
                {
                    try
                    {
                        _logger.Info($"Plugin {existingPlugin.PluginName} was changed, try to reload it...");

                        var deletedPlugin = await DeletePlugin(existingPlugin);
                        await UnloadPlugins(new List<DeletingPluginContainer> { deletedPlugin });
                        await LoadPlugin(existingPlugin, locators);

                        _logger.Info($"Plugin {existingPlugin.PluginName} successfully reloaded.");
                    }
                    catch (Exception e)
                    {
                        DeleteTempPlugin(existingPlugin);

                        _logger.Error(e);
                    }
                }
            }
        }

        private async Task<bool> PluginWasChanged(PluginContainer plugin)
        {
            var existingFiles = plugin.Files;
            var newFiles = plugin.TempFiles;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task LoadPlugin(PluginContainer plugin, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            var tempPlugin = await CopyPluginToTempDirectory(plugin);

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

                Assembly assembly = checkPluginResult.Assembly;

                var locatorType = typeof(IItemsLocator);

                var locatorTypes = assembly.GetTypes().Where(p => locatorType.IsAssignableFrom(p)).ToList();

                List<IItemsLocator> locs = new List<IItemsLocator>();

                foreach (var type in locatorTypes)
                {
                    var instance = (IItemsLocator)Activator.CreateInstance(type, _fabric);

                    locators.TryAdd(instance.ItemType, instance);
                    locs.Add(instance);

                    _logger.Info($"ItemLocator {instance.ItemType} has been created");
                }

                tempPlugin.Locators = locs;
                tempPlugin.AssemblyContext = context;
            }

            _knownPluginContainers.TryAdd(tempPlugin.PluginName, tempPlugin);

            _logger.Info($"Plugin {tempPlugin.PluginName} has been processed");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task UnloadPlugins(List<DeletingPluginContainer> deleteContainers)
        {
            if (!deleteContainers.Any())
                return;

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
                        }

                        if (!removingFailed)
                            deleteContainer.Plugin.Locators.Clear();

                        await Task.Delay(1000, _disposingCancellationTokenSource.Token);

                        if (!CollectGarbage(deleteContainer.Reference, deleteContainer.Plugin.PluginName))
                            throw new Exception("Could not unload dll. It's recommended to restart service.");

                        try
                        {
                            var tempPluginDirectoryPath = GetPluginPath(deleteContainer.Plugin);

                            await AsyncHelpers.RetryOnFault(
                                async () => Directory.Delete(tempPluginDirectoryPath, true), 5,
                                () => Task.Delay(1000, _disposingCancellationTokenSource.Token));
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e);
                        }

                        _logger.Info($"{deleteContainer.Plugin.PluginName} has been unloaded.");
                    }, _unloadPluginsMaxTries, () => Task.Delay(_unloadPluginsTriesIntervalMS, _disposingCancellationTokenSource.Token));
                }
                catch (Exception e)
                {
                    if (_softPluginsLoading)
                    {
                        _logger.Info("Could not unload dll but SoftPluginsLoading is True so continue working.");
                        //Try to clean as much as we can
                        try
                        {
                            DeleteTempPlugin(deleteContainer.Plugin);
                        }
                        catch (Exception exception)
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
                catch (BadImageFormatException e)
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
                return new LoadingPluginResult { Success = true, AssemblyContext = context, Assembly = assembly };

            context.Unload();

            var weakReference = new WeakReference(context);

            return new LoadingPluginResult { Success = false, Reference = weakReference };
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
            if (!_knownPluginContainers.TryGetValue(plugin.PluginName, out var deletedContainer))
                return null;

            _logger.Info($"Plugin {plugin.PluginName} has been deleted.");

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

            var container = new DeletingPluginContainer
                { Reference = weakReference, Plugin = deletedContainer };
            container.Plugin.AssemblyContext = null; //Delete reference to allow GC to make its work

            _logger.Info($"Assemblies from {plugin.PluginDirectoryInfo.FullName} are being unloaded.");

            _knownPluginContainers.TryRemove(plugin.PluginName, out var t);

            return container;
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

        private async Task<PluginContainer> CopyPluginToTempDirectory(PluginContainer plugin)
        {
            var tempPluginDirectoryPath = GetPluginPath(plugin);

            var tempFiles = new List<FileInfo>();

            Directory.CreateDirectory(tempPluginDirectoryPath);

            foreach (var file in plugin.Files)
            {
                //TODO refactor this to get rid of working with strings in path
                var relativePath = file.DirectoryName.Replace(plugin.PluginDirectoryInfo.FullName, "");

                if (relativePath.StartsWith(Path.DirectorySeparatorChar))
                    relativePath = relativePath.Remove(0, 1);

                var tempDirectoryPath = Path.Combine(tempPluginDirectoryPath, relativePath);
                Directory.CreateDirectory(tempDirectoryPath);
                var tempFilePath = Path.Combine(tempPluginDirectoryPath, relativePath, file.Name);

                try
                {
                    File.Copy(file.FullName, tempFilePath, true);
                }
                catch (Exception e)
                {
                    if (!_softPluginsLoading)
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

        public async Task<IEnumerable<IItemsLocator>> GetItemsLocators()
        {
            if (_isFirstRun)
                await _taskCompletionSource.Task;

            return _locators.Values;
        }

        private class PluginContainer
        {
            public string PluginName { get; set; }
            public DirectoryInfo PluginDirectoryInfo { get; set; }
            public List<FileInfo> Files { get; set; }
            public List<FileInfo> DllFiles { get; set; }

            public List<FileInfo> TempFiles { get; set; }
            public List<FileInfo> TempDllFiles { get; set; }

            public CollectibleAssemblyContext AssemblyContext { get; set; }
            public List<IItemsLocator> Locators { get; set; } = new List<IItemsLocator>();

            public override string ToString()
            {
                return PluginName;
            }
        }

        private class LoadingPluginResult
        {
            public bool Success { get; set; }
            public WeakReference Reference { get; set; }
            public CollectibleAssemblyContext AssemblyContext { get; set; }
            public Assembly Assembly { get; set; }
        }

        private class DeletingPluginContainer
        {
            public WeakReference Reference { get; set; }
            public PluginContainer Plugin { get; set; }
        }

        public void Dispose()
        {
            _disposingCancellationTokenSource.Cancel();

            _logger.Info("Disposing ItemsPluginsLocator...");

            DeletePlugins(_knownPluginContainers.Values.ToList()).Wait();

            _logger.Info("ItemsPluginsLocator has been disposed.");
        }
    }
}