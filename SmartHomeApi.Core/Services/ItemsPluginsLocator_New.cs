using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator_New : IItemsPluginsLocator
    {
        private string _pluginsDirectory;
        private string _tempPluginsDirectory;
        private bool _softPluginsLoading = true;
        private int _unloadPluginsMaxTries = 5;
        private int _unloadPluginsTriesIntervalMS = 500;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly IPluginsFileWatcher _fileWatcher;
        private readonly List<string> _librariesExtensions = new List<string> { ".dll" };

        private readonly ConcurrentDictionary<string, PluginContainer> _pluginContainers =
            new ConcurrentDictionary<string, PluginContainer>();

        private readonly ConcurrentDictionary<string, IItemsLocator>
            _locators = new ConcurrentDictionary<string, IItemsLocator>();

        public bool IsInitialized { get; private set; }

        public ItemsPluginsLocator_New(ISmartHomeApiFabric fabric)
        {
            var unused = typeof(AverageValuesHelper); //Workaround to load dll

            _semaphoreSlim = new SemaphoreSlim(1, 1);
            var config = fabric.GetConfiguration();

            EnsureDirectories(config);
            EnsureConfigParameters(config);

            _fabric = fabric;
            _logger = fabric.GetApiLogger();

            _fileWatcher = new PluginsFileWatcher(fabric, _pluginsDirectory, _librariesExtensions);
            _fileWatcher.PluginAddedOrUpdated += FileWatcherOnPluginAddedOrUpdated;
        }

        public async Task Initialize()
        {
            var initialPlugins = _fileWatcher.FindPlugins();

            await Task.WhenAll(initialPlugins.Select(ProcessPluginAddedOrUpdatedEvent));

            IsInitialized = true;
        }

        public async Task<IEnumerable<IItemsLocator>> GetItemsLocators()
        {
            return _locators.Values;
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
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

        private void EnsureConfigParameters(AppSettings config)
        {
            _softPluginsLoading = config.SoftPluginsLoading;
            _unloadPluginsMaxTries =
                config.UnloadPluginsMaxTries > 0 ? config.UnloadPluginsMaxTries : _unloadPluginsMaxTries;
            _unloadPluginsTriesIntervalMS = config.UnloadPluginsTriesIntervalMS > 0
                ? config.UnloadPluginsTriesIntervalMS
                : _unloadPluginsTriesIntervalMS;
        }

        private async void FileWatcherOnPluginAddedOrUpdated(object sender, PluginEventArgs e)
        {
            await ProcessPluginAddedOrUpdatedEvent(e);
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
                await LoadPlugin(pluginContainer);
            }
            catch (Exception e)
            {
                DeleteTempPlugin(pluginContainer);

                _logger.Error(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task LoadPlugin(PluginContainer plugin)
        {
            var tempPlugin = CopyPluginToTempDirectory(plugin);

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

                List<IItemsLocator> locs = new List<IItemsLocator>();

                foreach (var type in locatorTypes)
                {
                    var instance = (IItemsLocator)Activator.CreateInstance(type, _fabric);

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

                    _locators.TryAdd(instance.ItemType, instance);
                    locs.Add(instance);

                    _logger.Info($"ItemLocator {instance.ItemType} has been created");
                }

                tempPlugin.Locators = locs;
                tempPlugin.AssemblyContext = context;
            }

            _pluginContainers.TryAdd(tempPlugin.PluginDirectoryName, tempPlugin);

            _logger.Info($"Plugin {tempPlugin.PluginDirectoryName} has been processed");
        }

        private async Task UpdatePlugin(PluginContainer pluginContainer){}

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

        private void DeleteTempPlugin(PluginContainer plugin)
        {
            var tempPluginDirectoryPath = GetPluginPath(plugin);

            if (Directory.Exists(tempPluginDirectoryPath))
                Directory.Delete(tempPluginDirectoryPath, true);
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
    }
}