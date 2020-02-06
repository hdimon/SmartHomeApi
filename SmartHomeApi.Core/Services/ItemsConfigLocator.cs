using System;
using System.Collections.Concurrent;
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

        private ConcurrentDictionary<string, ConfigContainer> _configContainers =
            new ConcurrentDictionary<string, ConfigContainer>();

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
                var ext = new List<string> { ".json" };
                var files = Directory.EnumerateFiles(_configDirectory, "*.*", SearchOption.AllDirectories)
                                     .Where(s => ext.Contains(Path.GetExtension(s).ToLowerInvariant())).ToList();

                var newFiles = files.Except(_configContainers.Keys.ToList()).ToList();

                foreach (var newFile in newFiles)
                {
                    var container = new ConfigContainer();
                    container.FilePath = newFile;
                    container.Builder = new ConfigurationBuilder().AddJsonFile(newFile, optional: true);
                    container.Root = container.Builder.Build();

                    _configContainers.AddOrUpdate(newFile, s => container, (s, configContainer) => container);
                }

                foreach (var configContainer in _configContainers)
                {
                    SetItemConfig(configContainer.Value);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void SetItemConfig(ConfigContainer container)
        {
            string itemType = container.Root.GetValue<string>("ItemType", null);

            switch (itemType)
            {
                case "TerneoSx":
                    //container.ItemConfig = container.Root.Get<TerneoSxConfig>();
                    break;
            }
        }

        public List<IItemConfig> GetItemsConfigs(string itemType)
        {
            /*root.Reload();
            var ip = root.GetValue<string>("IpAddress");*/

            if (itemType == "TerneoSx")
                return new List<IItemConfig>
                {
                    new TerneoSxConfig("Kitchen_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.52", SerialNumber = "14000B000C43504735323620000159", Power = 1800},
                    new TerneoSxConfig("Bedroom_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.48", SerialNumber = "2B0008000C43504735323620000159", Power = 300},
                    new TerneoSxConfig("Bathroom_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.33", SerialNumber = "29000B000C43504735323620000159", Power = 150},
                    new TerneoSxConfig("Toilet_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.46", SerialNumber = "13001B000143504735323620000159", Power = 150 }
                };

            if (itemType == "VirtualStateDevice")
                return new List<IItemConfig>
                {
                    new VirtualStateConfig("Virtual_States", "VirtualStateDevice")
                };

            if (itemType == "VirtualAlarmClockDevice")
                return new List<IItemConfig>
                {
                    new VirtualAlarmClockConfig("Virtual_MainAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = true },
                    new VirtualAlarmClockConfig("Virtual_HeatingSystemMorningAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = false },
                    new VirtualAlarmClockConfig("Virtual_HeatingSystemAfterMorningAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = false },
                    new VirtualAlarmClockConfig("Virtual_TowelHeaterTurningOffAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = false }
                };

            if (itemType == "Mega2560Controller")
                return new List<IItemConfig>
                {
                    new Mega2560ControllerConfig("Kitchen_Mega2560", "Mega2560Controller")
                    {
                        Mac = "aa:bb:cc:00:00:01", IpAddress = "192.168.1.58", HasCO2Sensor = true,
                        HasTemperatureSensor = true, HasHumiditySensor = true, HasPressureSensor = true,
                        HasPins = true
                    },
                    new Mega2560ControllerConfig("Toilet_Mega2560", "Mega2560Controller")
                    {
                        Mac = "aa:bb:cc:00:00:02", IpAddress = "192.168.1.60", HasPins = true
                    },
                    new Mega2560ControllerConfig("Bedroom_Mega2560", "Mega2560Controller")
                    {
                        Mac = "aa:bb:cc:00:00:03", IpAddress = "192.168.1.61", HasSlave1CO2Sensor = true,
                        HasSlave1TemperatureSensor = true, HasSlave1HumiditySensor = true, HasSlave1PressureSensor = true,
                        HasPins = true
                    }
                };

            if (itemType == "BreezartLux550")
                return new List<IItemConfig>
                {
                    new BreezartLux550Config("Breezart", "BreezartLux550") { IpAddress = "192.168.1.37" }
                };

            if (itemType == "EventsPostgreSqlStorage")
                return new List<IItemConfig>
                {
                    new StorageConfig("MainPostgreStorage", "EventsPostgreSqlStorage")
                    {
                        ConnectionString =
                            "User ID=postgres;Password=admin19;Host=localhost;Port=5432;Database=SmartHomeApi;Pooling=true;"
                    }
                };

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