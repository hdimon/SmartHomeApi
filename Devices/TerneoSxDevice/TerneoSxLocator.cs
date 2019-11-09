using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;

namespace TerneoSxDevice
{
    public class TerneoSxLocator : IDeviceLocator
    {
        public string DeviceType => "TerneoSx";

        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IDevice> _devices = new ConcurrentDictionary<string, IDevice>();

        public TerneoSxLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public List<IDevice> GetDevices()
        {
            var configLocator = _fabric.GetDeviceConfigsLocator();
            var helpersFabric = _fabric.GetDeviceHelpersFabric();

            var configs = configLocator.GetDeviceConfigs(DeviceType);

            foreach (var config in configs)
            {
                if (_devices.ContainsKey(config.DeviceId))
                    continue; //Update config

                _devices.TryAdd(config.DeviceId, new TerneoSx(helpersFabric, config));
            }

            //Remove configs

            return _devices.Values.ToList();
        }
    }
}