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
using SmartHomeApi.DeviceUtils;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator : IItemsPluginsLocator
    {
        private string _pluginsDirectory;
        private string _tempPluginsDirectory;
        private Task _worker;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private volatile bool _isFirstRun = true;

        private ConcurrentDictionary<string, IItemsLocator> _locators = new ConcurrentDictionary<string, IItemsLocator>();

        private readonly ConcurrentDictionary<string, PluginContainer> _knownPluginContainers =
            new ConcurrentDictionary<string, PluginContainer>();

        public ItemsPluginsLocator(ISmartHomeApiFabric fabric)
        {
            _pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            _tempPluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TempPlugins");

            Directory.Delete(_tempPluginsDirectory, true);
            Directory.CreateDirectory(_tempPluginsDirectory);

            _fabric = fabric;
            _logger = fabric.GetApiLogger();

            RunPluginsCollectorWorker();

            var test = typeof(AverageValuesHelper);
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
            while (true)
            {
                if (!_isFirstRun)
                    await Task.Delay(1000);

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
            var pluginFileContainers = await CollectPluginContainers(_pluginsDirectory);
            var tempPluginFileContainers = await CollectPluginContainers(_tempPluginsDirectory);

            var deletedDirectories = tempPluginFileContainers
                                     .Select(c => c.PluginDirectoryName)
                                     .Except(pluginFileContainers.Select(c => c.PluginDirectoryName)).ToList();
            var addedDirectories = pluginFileContainers
                                   .Select(c => c.PluginDirectoryName)
                                   .Except(tempPluginFileContainers.Select(c => c.PluginDirectoryName)).ToList();

            var deletedPlugins = tempPluginFileContainers
                                 .Where(p => deletedDirectories.Contains(p.PluginDirectoryName)).ToList();
            var addedPlugins = pluginFileContainers
                               .Where(p => addedDirectories.Contains(p.PluginDirectoryName)).ToList();
            var existingPlugins = tempPluginFileContainers.Except(deletedPlugins).ToList();

            await UpdatePlugins(existingPlugins, locators);
            await AddPlugins(addedPlugins, locators);

            var deleteContainers = await DeletePlugins(deletedPlugins);

            await UnloadPlugins(deleteContainers);
        }

        private async Task<List<PluginContainer>> CollectPluginContainers(string directory)
        {
            var ext = new List<string> { ".dll" };

            var pluginFileContainers = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                                                .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant()))
                                                .Select(p => new PluginContainer { FilePath = p }).ToList();

            foreach (var container in pluginFileContainers)
            {
                var file = new FileInfo(container.FilePath);
                container.PluginDirectoryName = file.Directory?.Name;
                container.PluginDirectoryPath = file.Directory?.FullName;
            }

            pluginFileContainers = pluginFileContainers
                                   .Where(p => p.PluginDirectoryName != null)
                                   .GroupBy(p => p.PluginDirectoryName)
                                   .Select(g => new PluginContainer
                                   {
                                       FilePathes = g.Select(p => p.FilePath).ToList(),
                                       PluginDirectoryName = g.First().PluginDirectoryName,
                                       PluginDirectoryPath = g.First().PluginDirectoryPath
                                   }).ToList();

            return pluginFileContainers;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task AddPlugins(List<PluginContainer> plugins, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            foreach (var pluginContainer in plugins)
            {
                await LoadPlugin(pluginContainer, locators);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task UpdatePlugins(List<PluginContainer> plugins, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            foreach (var plugin in plugins)
            {
                if (!_knownPluginContainers.ContainsKey(plugin.PluginDirectoryName)) 
                    continue;

                var existingPlugin = _knownPluginContainers[plugin.PluginDirectoryName];

                if (!await PluginWasChanged(existingPlugin))
                {
                    foreach (var itemsLocator in existingPlugin.Locators)
                    {
                        locators.TryAdd(itemsLocator.ItemType, itemsLocator);
                    }
                }
                else
                {
                    _logger.Info($"Plugin {existingPlugin.PluginDirectoryName} was changed, try to reload it...");

                    var deletedPlugin = await DeletePlugin(existingPlugin);
                    await UnloadPlugins(new List<DeletingPluginContainer> { deletedPlugin });
                    await LoadPlugin(existingPlugin, locators);

                    _logger.Info($"Plugin {existingPlugin.PluginDirectoryName} successfully reloaded.");
                }
            }
        }

        private async Task<bool> PluginWasChanged(PluginContainer plugin)
        {
            var existingDllPathes = plugin.TempFilePathes;
            var newDllPathes = plugin.FilePathes;

            if (existingDllPathes.Count != newDllPathes.Count)
                return true;

            var newDllsPathesByNames =
                newDllPathes.Where(p => Path.GetFileName(p) != null).ToDictionary(Path.GetFileName, s => s);

            foreach (var existingDllPath in existingDllPathes)
            {
                var existingDllName = Path.GetFileName(existingDllPath);

                if (!newDllsPathesByNames.ContainsKey(existingDllName))
                    return true;

                var newDllPath = newDllsPathesByNames[existingDllName];

                if (new FileInfo(existingDllPath).Length != new FileInfo(newDllPath).Length)
                    return true;

                var existingDllBytes = await File.ReadAllBytesAsync(existingDllPath);
                var newDllBytes = await File.ReadAllBytesAsync(newDllPath);

                for (long i = 0; i < existingDllBytes.LongLength; i++)
                {
                    if (existingDllBytes[i] != newDllBytes[i])
                        return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task LoadPlugin(PluginContainer plugin, ConcurrentDictionary<string, IItemsLocator> locators)
        {
            var tempPlugin = await CopyPluginToTempDirectory(plugin);

            foreach (var dllPath in tempPlugin.TempFilePathes)
            {
                var checkPluginResult = LoadDllIfPlugin(dllPath);

                if (!checkPluginResult.Success)
                {
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

            _knownPluginContainers.TryAdd(tempPlugin.PluginDirectoryName, tempPlugin);

            _logger.Info($"Plugin {tempPlugin.PluginDirectoryName} has been processed");
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
                            if (!_locators.TryRemove(itemsLocator.ItemType, out var itemsLoc))
                                removingFailed = true;
                        }

                        if (!removingFailed)
                            deleteContainer.Plugin.Locators.Clear();

                        await Task.Delay(1000);

                        if (!CollectGarbage(deleteContainer.Reference, deleteContainer.Plugin.PluginDirectoryName))
                            throw new Exception("Could not unload dll. It's recommended to restart service.");

                        try
                        {
                            await AsyncHelpers.RetryOnFault(
                                async () => Directory.Delete(deleteContainer.Plugin.TempPluginDirectoryPath, true), 5,
                                () => Task.Delay(1000));
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e);
                        }

                        _logger.Info($"{deleteContainer.Plugin.PluginDirectoryName} has been unloaded.");
                    }, 5, () => Task.Delay(1000));
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }

            _logger.Info("Assemblies unloading have been finished.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private LoadingPluginResult LoadDllIfPlugin(string dllPath)
        {
            var context = new CollectibleAssemblyContext(dllPath);

            Assembly assembly;

            //Load from FileStream instead of by AssemblyName because it never blocks dll.
            using (var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            {
                assembly = context.LoadFromStream(fs);
            }

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
            if (!_knownPluginContainers.TryGetValue(plugin.PluginDirectoryName, out var deletedContainer))
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

            var container = new DeletingPluginContainer
                { Reference = weakReference, Plugin = deletedContainer };
            container.Plugin.AssemblyContext = null; //Delete reference to allow GC to make its work

            _logger.Info($"Assemblies from {plugin.PluginDirectoryPath} are being unloaded.");

            _knownPluginContainers.TryRemove(plugin.PluginDirectoryName, out var t);

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
            var ext = new List<string> { ".dll" };

            var tempPluginDirectoryPath = Path.Combine(_tempPluginsDirectory, plugin.PluginDirectoryName);

            FileHelper.Copy(plugin.PluginDirectoryPath, tempPluginDirectoryPath);

            plugin.TempPluginDirectoryPath = tempPluginDirectoryPath;
            plugin.TempFilePathes = Directory
                                    .EnumerateFiles(tempPluginDirectoryPath, "*.*", SearchOption.AllDirectories)
                                    .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())).ToList();

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
            public string FilePath { get; set; }
            public List<string> FilePathes { get; set; }
            public string PluginDirectoryName { get; set; }
            public string PluginDirectoryPath { get; set; }

            public List<string> TempFilePathes { get; set; }
            public string TempPluginDirectoryPath { get; set; }

            public CollectibleAssemblyContext AssemblyContext { get; set; }
            public List<IItemsLocator> Locators { get; set; }
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
    }
}