using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BreezartLux550Device;
using EventsPostgreSqlStorage;
using Mega2560ControllerDevice;
using Microsoft.Extensions.Configuration;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class ItemsConfigLocator : IItemsConfigLocator
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;
        private Task _worker;
        private string _configDirectory;

        private Dictionary<string, ConfigContainer> _configContainers = new Dictionary<string, ConfigContainer>();

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
                    SetItemConfig(configContainer.Value);
                }

                _configContainers = configContainers;
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void SetItemConfig(ConfigContainer container)
        {
            string itemType = container.Root.GetValue<string>("ItemType", null);
            string itemId = container.Root.GetValue<string>("ItemId", null);

            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(itemId))
                return;

            IItemConfig config = GetItemConfig(itemType, itemId, container);

            if (config == null)
                return;

            container.Root.Bind(config);
            container.ItemConfig = config;
        }

        private IItemConfig GetItemConfig(string itemType, string itemId, ConfigContainer container)
        {
            IItemConfig config = null;

            switch (itemType)
            {
                case "TerneoSx":
                    config = container.ItemConfig ?? new TerneoSxConfig(itemId, itemType);
                    break;
                case "VirtualStateDevice":
                    config = container.ItemConfig ?? new VirtualStateConfig(itemId, itemType);
                    break;
                case "VirtualAlarmClockDevice":
                    config = container.ItemConfig ?? new VirtualAlarmClockConfig(itemId, itemType);
                    break;
                case "BreezartLux550":
                    config = container.ItemConfig ?? new BreezartLux550Config(itemId, itemType);
                    break;
                case "EventsPostgreSqlStorage":
                    config = container.ItemConfig ?? new StorageConfig(itemId, itemType);
                    break;
                case "Mega2560Controller":
                    config = container.ItemConfig ?? new Mega2560ControllerConfig(itemId, itemType);
                    break;
            }

            return config;
        }

        public List<IItemConfig> GetItemsConfigs(string itemType)
        {
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