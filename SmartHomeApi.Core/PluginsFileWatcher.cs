using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class PluginsFileWatcher : IPluginsFileWatcher
    {
        private const int PluginsLoadingTimeMsDefault = 5000;

        private readonly string _pluginsPath;
        private readonly List<string> _librariesExtensions;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly FileSystemWatcher _watcher;

        private readonly ConcurrentDictionary<string, DirectoryProcessed> _directories =
            new ConcurrentDictionary<string, DirectoryProcessed>();

        public event EventHandler<PluginEventArgs> PluginAddedOrUpdated;
        //public event EventHandler<PluginEventArgs> PluginUpdated;
        public event EventHandler<PluginEventArgs> PluginDeleted;

        public PluginsFileWatcher(ISmartHomeApiFabric fabric, string pluginsPath, List<string> librariesExtensions)
        {
            _fabric = fabric;
            _logger = fabric.GetApiLogger();
            _pluginsPath = pluginsPath;
            _librariesExtensions = librariesExtensions;

            _watcher = new FileSystemWatcher(_pluginsPath);
            _watcher.Created += WatcherOnCreated;

            //Since actually we support only .dll (even though "List<string> librariesExtensions" passed)
            //and _watcher.Filter does not support multiple extensions then now we create only one FileWatcher for .dll
            _watcher.Filter = "*.dll";
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        public IList<PluginEventArgs> FindPlugins()
        {
            var plugins = new List<PluginEventArgs>();

            if (!Directory.Exists(_pluginsPath))
                return plugins;

            var pluginDirectories = Directory.EnumerateDirectories(_pluginsPath).ToList();

            foreach (var pluginDirectoryPath in pluginDirectories)
            {
                var plugin = ProcessDirectory(pluginDirectoryPath, false);

                if (plugin != null)
                    plugins.Add(plugin);
            }

            return plugins;
        }

        private PluginEventArgs ProcessDirectory(string pluginDirectoryPath, bool raiseEvent = true)
        {
            if (!Directory.Exists(pluginDirectoryPath))
                return null;

            var files = Directory.EnumerateFiles(pluginDirectoryPath, "*.*", SearchOption.AllDirectories)
                                 .Select(f => new FileInfo(f)).ToList();

            if (!files.Any())
                return null;

            var directoryInfo = new DirectoryInfo(pluginDirectoryPath);

            var eventArg = new PluginEventArgs();
            eventArg.PluginDirectoryName = directoryInfo.Name;
            eventArg.PluginDirectoryInfo = directoryInfo;
            eventArg.Files = files;
            eventArg.DllFiles = files.Where(f => _librariesExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()))
                                     .ToList();

            if (raiseEvent)
                OnPluginAddedOrUpdated(eventArg);

            return eventArg;
        }

        private void OnPluginAddedOrUpdated(PluginEventArgs e)
        {
            PluginAddedOrUpdated?.Invoke(this, e);
        }

        private void OnPluginDeleted(PluginEventArgs e)
        {
            PluginDeleted?.Invoke(this, e);
        }

        private void WatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                var pluginDirectoryPath = Path.GetDirectoryName(e.FullPath);

                if (pluginDirectoryPath == null)
                    return;

                if (_directories.ContainsKey(pluginDirectoryPath))
                    return; //It means this plugin is being processed now.

                //This is needed for case when two or more files were copied to folder at the same time and thus
                //all files in different threads passed previous check. isNewPlugin variable allows to make sure that
                //ProcessDirectoryWithDelay will be called only once. (At least I hope so.)
                bool isNewPlugin = true;

                _directories.AddOrUpdate(pluginDirectoryPath,
                    new DirectoryProcessed { PluginDirectoryPath = pluginDirectoryPath }, (key, dir) =>
                    {
                        isNewPlugin = false;
                        return dir;
                    });

                if (isNewPlugin)
                    Task.Run(async () => await ProcessDirectoryWithDelay(pluginDirectoryPath)).ContinueWith(t =>
                        {
                            _directories.TryRemove(pluginDirectoryPath, out _);
                            _logger.Error(t.Exception);
                        },
                        TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
        }

        private async Task ProcessDirectoryWithDelay(string pluginDirectoryPath)
        {
            var pluginsLoadingTimeMs = GetPluginsLoadingTimeMs();

            _logger.Info($"Plugin directory {pluginDirectoryPath} will be processed in {pluginsLoadingTimeMs} ms.");

            await Task.Delay(GetPluginsLoadingTimeMs());

            ProcessDirectory(pluginDirectoryPath);

            _directories.TryRemove(pluginDirectoryPath, out _);
        }

        private int GetPluginsLoadingTimeMs()
        {
            var pluginsLoadingTimeMs = _fabric.GetConfiguration().PluginsLoadingTimeMs;

            return pluginsLoadingTimeMs == 0 ? PluginsLoadingTimeMsDefault : pluginsLoadingTimeMs;
        }

        private class DirectoryProcessed
        {
            public string PluginDirectoryPath { get; set; }
            //public CancellationTokenSource Cts { get; set; }
        }
    }
}