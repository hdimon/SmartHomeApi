using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Mega2560ControllerDevice
{
    public class Mega2560ControllerLocator : IItemsLocator
    {
        public string ItemType => "Mega2560Controller";
        public bool ImmediateInitialization => false;

        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IItem> _devices = new ConcurrentDictionary<string, IItem>();

        public Mega2560ControllerLocator(ISmartHomeApiFabric fabric)
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

                _devices.TryAdd(config.ItemId, new Mega2560Controller(helpersFabric, config));
            }

            //Remove configs

            return _devices.Values;
        }
    }
}