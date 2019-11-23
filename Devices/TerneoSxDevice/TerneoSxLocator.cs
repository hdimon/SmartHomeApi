using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace TerneoSxDevice
{
    public class TerneoSxLocator : IItemsLocator
    {
        public string ItemType => "TerneoSx";

        public bool ImmediateInitialization => false;

        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IItem> _devices = new ConcurrentDictionary<string, IItem>();

        public TerneoSxLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            var configLocator = _fabric.GetDeviceConfigsLocator();
            var helpersFabric = _fabric.GetDeviceHelpersFabric();

            var configs = configLocator.GetDeviceConfigs(ItemType);

            foreach (var config in configs)
            {
                if (_devices.ContainsKey(config.DeviceId))
                    continue; //Update config

                _devices.TryAdd(config.DeviceId, new TerneoSx(helpersFabric, config));
            }

            //Remove configs

            return _devices.Values;
        }
    }
}