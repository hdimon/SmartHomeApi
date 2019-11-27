using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreezartLux550Device;
using EventsPostgreSqlStorage;
using Mega2560ControllerDevice;
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
            var mega2560Controller = new Mega2560ControllerLocator(_fabric);
            var breezart = new BreezartLux550Locator(_fabric);
            var eventsStorage = new StorageLocator(_fabric);

            _locators.TryAdd(terneo.ItemType, terneo);
            _locators.TryAdd(virtualState.ItemType, virtualState);
            _locators.TryAdd(virtualAlarmClock.ItemType, virtualAlarmClock);
            _locators.TryAdd(scenarios.ItemType, scenarios);
            _locators.TryAdd(mega2560Controller.ItemType, mega2560Controller);
            _locators.TryAdd(breezart.ItemType, breezart);
            _locators.TryAdd(eventsStorage.ItemType, eventsStorage);
        }

        public async Task<IEnumerable<IItemsLocator>> GetItemsLocators()
        {
            return _locators.Values;
        }
    }
}