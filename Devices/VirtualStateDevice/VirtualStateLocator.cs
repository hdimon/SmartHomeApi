using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace VirtualStateDevice
{
    public class VirtualStateLocator : IItemsLocator
    {
        public string ItemType => "VirtualStateDevice";

        public bool ImmediateInitialization => true;

        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IItem> _devices = new ConcurrentDictionary<string, IItem>();

        public VirtualStateLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            var configLocator = _fabric.GetItemsConfigsLocator();
            var helpersFabric = _fabric.GetItemHelpersFabric();

            var configs = configLocator.GetItemsConfigs(ItemType);

            foreach (var config in configs)
            {
                if (_devices.ContainsKey(config.ItemId))
                    continue; //Update config

                _devices.TryAdd(config.ItemId, new VirtualState(helpersFabric, config));
            }

            //Remove configs

            return _devices.Values;
        }
    }
}