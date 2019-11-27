using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace BreezartLux550Device
{
    public class BreezartLux550Locator : IItemsLocator
    {
        public string ItemType => "BreezartLux550";
        public bool ImmediateInitialization => false;

        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IItem> _devices = new ConcurrentDictionary<string, IItem>();

        public BreezartLux550Locator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            var configLocator = _fabric.GetDeviceConfigsLocator();
            var helpersFabric = _fabric.GetDeviceHelpersFabric();

            var configs = configLocator.GetItemsConfigs(ItemType);

            foreach (var config in configs)
            {
                if (_devices.ContainsKey(config.ItemId))
                    continue; //Update config

                _devices.TryAdd(config.ItemId, new BreezartLux550(helpersFabric, config));
            }

            //Remove configs

            return _devices.Values;
        }
    }
}