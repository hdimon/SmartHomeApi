using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ItemsLocator : IItemsLocator
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;

        public string ItemType => null;
        public bool ImmediateInitialization => false;

        public ItemsLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();

            var items = new List<IItem>();

            foreach (var locator in locators)
            {
                /*var deviceConfigs = configs.Where(c => string.Equals(c.DeviceType, locator.DeviceType,
                    StringComparison.InvariantCultureIgnoreCase)).ToList();*/

                try
                {
                    items.AddRange(await locator.GetItems());
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error when collecting items.");
                }
            }

            return items;
        }
    }
}