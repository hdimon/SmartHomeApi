using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class DevicePluginLocator : IDevicePluginLocator
    {
        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IDeviceLocator> _locators =
            new ConcurrentDictionary<string, IDeviceLocator>();

        public DevicePluginLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;

            var terneo = new TerneoSxLocator(_fabric);
            var virtualState = new VirtualStateLocator(_fabric);
            var virtualAlarmClock = new VirtualAlarmClockLocator(_fabric);

            _locators.TryAdd(terneo.DeviceType, terneo);
            _locators.TryAdd(virtualState.DeviceType, virtualState);
            _locators.TryAdd(virtualAlarmClock.DeviceType, virtualAlarmClock);
        }

        public List<IDeviceLocator> GetDeviceLocators()
        {
            return _locators.Values.ToList();
        }
    }
}