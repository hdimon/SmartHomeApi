using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Scenarios;
using SmartHomeApi.Core.Interfaces;
using TerneoSxDevice;
using VirtualAlarmClockDevice;
using VirtualStateDevice;

namespace SmartHomeApi.Core.Services
{
    public class ItemsPluginsLocator : IItemsPluginsLocator
    {
        private readonly ISmartHomeApiFabric _fabric;

        private readonly ConcurrentDictionary<string, IItemsLocator> _locators =
            new ConcurrentDictionary<string, IItemsLocator>();

        public ItemsPluginsLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;

            var terneo = new TerneoSxLocator(_fabric);
            var virtualState = new VirtualStateLocator(_fabric);
            var virtualAlarmClock = new VirtualAlarmClockLocator(_fabric);
            var scenarios = new ScenariosLocator(_fabric);

            _locators.TryAdd(terneo.ItemType, terneo);
            _locators.TryAdd(virtualState.ItemType, virtualState);
            _locators.TryAdd(virtualAlarmClock.ItemType, virtualAlarmClock);
            _locators.TryAdd(scenarios.ItemType, scenarios);
        }

        public async Task<IEnumerable<IItemsLocator>> GetItemsLocators()
        {
            return _locators.Values;
        }
    }
}