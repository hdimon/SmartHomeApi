using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ItemsLocator : IItemsLocator
    {
        private readonly ISmartHomeApiFabric _fabric;

        public string ItemType => null;
        public bool ImmediateInitialization => false;

        public ItemsLocator(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
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
                    /*Console.WriteLine(e);
                    throw;*/
                }
            }

            return items;
        }
    }
}