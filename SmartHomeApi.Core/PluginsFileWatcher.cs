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
        public event EventHandler<PluginEventArgs> PluginDeleted;

        public PluginsFileWatcher(ISmartHomeApiFabric fabric, string pluginsPath, List<string> librariesExtensions)
        {
            _fabric = fabric;
            _logger = fabric.GetApiLogger();
            _pluginsPath = pluginsPath;
            _librariesExtensions = librariesExtensions;

            _watcher = new FileSystemWatcher(_pluginsPath);

            _watcher.Created += WatcherOnCreated;
            _watcher.Changed += WatcherOnChanged;
            _watcher.Deleted += WatcherOnDeleted;

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
            var directoryInfo = new DirectoryInfo(pluginDirectoryPath);
            var eventArg = new PluginEventArgs();

            if (!Directory.Exists(pluginDirectoryPath))
            {
                if (!_directories.TryGetValue(pluginDirectoryPath, out var directoryProcessed))
                    return null;

                eventArg.PluginDirectoryName = directoryInfo.Name;
                eventArg.PluginDirectoryInfo = directoryInfo;

                _directories.TryRemove(pluginDirectoryPath, out _);

                OnPluginDeleted(eventArg);

                return null;
            }

            var files = Directory.EnumerateFiles(pluginDirectoryPath, "*.*", SearchOption.AllDirectories)
                                 .Select(f => new FileInfo(f)).ToList();

            eventArg.PluginDirectoryName = directoryInfo.Name;
            eventArg.PluginDirectoryInfo = directoryInfo;
            eventArg.Files = files;
            eventArg.DllFiles = files.Where(f => _librariesExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()))
                                     .ToList();

            _directories.TryRemove(pluginDirectoryPath, out _);

            if (raiseEvent)
            {
                if (eventArg.DllFiles.Any())
                    OnPluginAddedOrUpdated(eventArg);
                else
                    OnPluginDeleted(eventArg);
            }

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
                var pluginDirectoryPath = IsDirectory(e.FullPath) ? e.FullPath : Path.GetDirectoryName(e.FullPath);

                if (pluginDirectoryPath == null)
                    return;

                if (_directories.ContainsKey(pluginDirectoryPath))
                    return; //It means this plugin is being processed now.

                //This is needed for case when two or more files were copied to folder at the same time and thus
                //all files in different threads passed previous check. isNewPlugin variable allows to make sure that
                //ProcessDirectoryWithDelay will be called only once. (At least I hope so.)
                bool isNewProcessedPlugin = true;

                _directories.AddOrUpdate(pluginDirectoryPath,
                    new DirectoryProcessed { PluginDirectoryPath = pluginDirectoryPath, EventType = eventType }, (key, dir) =>
                    {
                        isNewProcessedPlugin = false;
                        return dir;
                    });

                if (isNewProcessedPlugin)
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
        }

        private int GetPluginsLoadingTimeMs()
        {
            var pluginsLoadingTimeMs = _fabric.GetConfiguration().ItemsPluginsLocator.PluginsLoadingTimeMs;

            return pluginsLoadingTimeMs == 0 ? PluginsLoadingTimeMsDefault : pluginsLoadingTimeMs;
        }

        private bool IsDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException();

            path = path.Trim();

            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;

            if (new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Any(x => path.EndsWith(x)))
                return true;

            return string.IsNullOrWhiteSpace(Path.GetExtension(path));
        }

        private class DirectoryProcessed
        {
            public string PluginDirectoryPath { get; set; }
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