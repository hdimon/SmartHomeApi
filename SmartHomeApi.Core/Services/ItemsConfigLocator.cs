using System.Collections.Generic;
using BreezartLux550Device;
using EventsPostgreSqlStorage;
using Mega2560ControllerDevice;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class ItemsConfigLocator : IItemsConfigLocator
    {
        public List<IItemConfig> GetItemsConfigs(string itemType)
        {
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
                        Mac = "aa:bb:cc:00:00:01", IpAddress = "192.168.1.58", HasCO2Sensor = false,
                        HasTemperatureSensor = true, HasHumiditySensor = true, HasPressureSensor = true,
                        HasPins = true
                    },
                    new Mega2560ControllerConfig("Toilet_Mega2560", "Mega2560Controller")
                    {
                        Mac = "aa:bb:cc:00:00:02", IpAddress = "192.168.1.60", HasCO2Sensor = false,
                        HasTemperatureSensor = false, HasHumiditySensor = false, HasPressureSensor = false,
                        HasPins = true
                    },
                    new Mega2560ControllerConfig("Bedroom_Mega2560", "Mega2560Controller")
                    {
                        Mac = "aa:bb:cc:00:00:03", IpAddress = "192.168.1.61", HasCO2Sensor = true,
                        HasTemperatureSensor = true, HasHumiditySensor = true, HasPressureSensor = true,
                        HasPins = false
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
    }
}