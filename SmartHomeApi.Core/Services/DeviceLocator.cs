using System;
using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class DeviceLocator : IDeviceLocator
    {
        private readonly ISmartHomeApiFabric _fabric;

        public string DeviceType => null;

        public DeviceLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public List<IDevice> GetDevices()
        {
            var locators =  _fabric.GetDevicePluginLocator().GetDeviceLocators();

            var devices = new List<IDevice>();

            foreach (var locator in locators)
            {
                /*var deviceConfigs = configs.Where(c => string.Equals(c.DeviceType, locator.DeviceType,
                    StringComparison.InvariantCultureIgnoreCase)).ToList();*/

                try
                {
                    devices.AddRange(locator.GetDevices());
                }
                catch (Exception e)
                {
                    /*Console.WriteLine(e);
                    throw;*/
                }
            }

            return devices;
        }
    }
}