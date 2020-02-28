﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventsPostgreSqlStorage;
using Mega2560ControllerDevice;
using Scenarios;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator : IItemsPluginsLocator
    {
        private string _pluginsDirectory;
        private Task _worker;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private volatile bool _isFirstRun = true;

        TerneoSxLocator terneo;
        VirtualStateLocator virtualState;
        VirtualAlarmClockLocator virtualAlarmClock;
        ScenariosLocator scenarios;
        Mega2560ControllerLocator mega2560Controller;
        StorageLocator eventsStorage;

        private Dictionary<string, IItemsLocator> _locators = new Dictionary<string, IItemsLocator>();
        private readonly ConcurrentDictionary<string, PluginContainer> _knownPlugins =
            new ConcurrentDictionary<string, PluginContainer>();

        public ItemsPluginsLocator(ISmartHomeApiFabric fabric)
        {
            _pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            _fabric = fabric;
            _logger = fabric.GetApiLogger();

            RunPluginsCollectorWorker();

            terneo = new TerneoSxLocator(_fabric);
            virtualState = new VirtualStateLocator(_fabric);
            virtualAlarmClock = new VirtualAlarmClockLocator(_fabric);
            scenarios = new ScenariosLocator(_fabric);
            mega2560Controller = new Mega2560ControllerLocator(_fabric);
            eventsStorage = new StorageLocator(_fabric);
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
            var ext = new List<string> { ".dll" };
            var pluginFiles = Directory.EnumerateFiles(_pluginsDirectory, "*.*", SearchOption.AllDirectories)
                                       .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())).ToList();

            var locs = new Dictionary<string, IItemsLocator>();

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    await ProcessPlugin(pluginFile, locs);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }

            if (!_locators.ContainsKey(terneo.ItemType))
                locs.Add(terneo.ItemType, terneo);
            else
                locs.Add(terneo.ItemType, _locators[terneo.ItemType]);
            if (!_locators.ContainsKey(virtualState.ItemType))
                locs.Add(virtualState.ItemType, virtualState);
            else
                locs.Add(virtualState.ItemType, _locators[virtualState.ItemType]);
            if (!_locators.ContainsKey(virtualAlarmClock.ItemType))
                locs.Add(virtualAlarmClock.ItemType, virtualAlarmClock);
            else
                locs.Add(virtualAlarmClock.ItemType, _locators[virtualAlarmClock.ItemType]);
            if (!_locators.ContainsKey(scenarios.ItemType))
                locs.Add(scenarios.ItemType, scenarios);
            else
                locs.Add(scenarios.ItemType, _locators[scenarios.ItemType]);
            if (!_locators.ContainsKey(mega2560Controller.ItemType))
                locs.Add(mega2560Controller.ItemType, mega2560Controller);
            else
                locs.Add(mega2560Controller.ItemType, _locators[mega2560Controller.ItemType]);
            if (!_locators.ContainsKey(eventsStorage.ItemType))
                locs.Add(eventsStorage.ItemType, eventsStorage);
            else
                locs.Add(eventsStorage.ItemType, _locators[eventsStorage.ItemType]);

            //Make sure that ref is not cached
            Interlocked.Exchange(ref _locators, locs);

            if (_isFirstRun)
            {
                _taskCompletionSource.SetResult(true);
                _isFirstRun = false;
            }
        }

        private async Task ProcessPlugin(string pluginFile, Dictionary<string, IItemsLocator> locators)
        {
            if (_knownPlugins.ContainsKey(pluginFile))
            {
                foreach (var itemsLocator in _knownPlugins[pluginFile].Locators)
                {
                    locators.Add(itemsLocator.ItemType, itemsLocator);
                }

                return;
            }

            var context = new CollectibleAssemblyContext();
            var assemblyPath = Path.Combine(pluginFile);

            using (var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            {
                var assembly = context.LoadFromStream(fs);

                var locatorType = typeof(IItemsLocator);

                var locatorTypes = assembly.GetTypes().Where(p => locatorType.IsAssignableFrom(p)).ToList();

                List<IItemsLocator> locs = new List<IItemsLocator>();

                foreach (var type in locatorTypes)
                {
                    var instance = (IItemsLocator)Activator.CreateInstance(type, _fabric);

                    locators.Add(instance.ItemType, instance);
                    locs.Add(instance);

                    _logger.Info($"ItemLocator {instance.ItemType} has been created");
                }

                _knownPlugins.TryAdd(pluginFile, new PluginContainer { FilePath = pluginFile, AssemblyContext = context, Locators = locs });

                _logger.Info($"Plugin {pluginFile} has been processed");
                //context.Unload();
            }
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
            public CollectibleAssemblyContext AssemblyContext { get; set; }
            public List<IItemsLocator> Locators { get; set; }
        }
    }
}