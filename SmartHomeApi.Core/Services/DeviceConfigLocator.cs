﻿using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class DeviceConfigLocator : IDeviceConfigLocator
    {
        public List<IDeviceConfig> GetDeviceConfigs(string deviceType)
        {
            if (deviceType == "TerneoSx")
                return new List<IDeviceConfig>
                {
                    new TerneoSxConfig("Kitchen_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.52", SerialNumber = "14000B000C43504735323620000159" },
                    new TerneoSxConfig("Bedroom_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.48", SerialNumber = "2B0008000C43504735323620000159" },
                    new TerneoSxConfig("Bathroom_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.33", SerialNumber = "29000B000C43504735323620000159" },
                    new TerneoSxConfig("Toilet_Floor", "TerneoSx")
                        { IpAddress = "192.168.1.46", SerialNumber = "13001B000143504735323620000159" }
                };

            if (deviceType == "VirtualStateDevice")
                return new List<IDeviceConfig>
                {
                    new VirtualStateConfig("Virtual_States", "VirtualStateDevice")
                };

            if (deviceType == "VirtualAlarmClockDevice")
                return new List<IDeviceConfig>
                {
                    new VirtualAlarmClockConfig("Virtual_MainAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = true },
                    new VirtualAlarmClockConfig("Virtual_HeatingSystemMorningAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = false },
                    new VirtualAlarmClockConfig("Virtual_HeatingSystemAfterMorningAlarmClock", "VirtualAlarmClockDevice")
                        { EveryDay = false }
                };

            return new List<IDeviceConfig>();
        }
    }
}