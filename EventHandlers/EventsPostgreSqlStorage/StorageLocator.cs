using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    public class StorageLocator : IItemsLocator
    {
        private readonly ISmartHomeApiFabric _fabric;
        public string ItemType => "EventsPostgreSqlStorage";
        public bool ImmediateInitialization => true;

        private readonly ConcurrentDictionary<string, IItem> _devices = new ConcurrentDictionary<string, IItem>();

        public StorageLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            var configLocator = _fabric.GetItemsConfigsLocator();
            var manager = _fabric.GetApiManager();

            var configs = configLocator.GetItemsConfigs(ItemType);

            foreach (var config in configs)
            {
                if (_devices.ContainsKey(config.ItemId))
                    continue; //Update config

                _devices.TryAdd(config.ItemId, new Storage(manager, _fabric.GetItemHelpersFabric(), config));
            }

            //Remove configs

            return _devices.Values;
        }
    }
}