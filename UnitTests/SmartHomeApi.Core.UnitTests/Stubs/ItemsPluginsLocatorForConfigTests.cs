using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class ItemsPluginsLocatorForConfigTests : IItemsPluginsLocator
    {
        public List<IItemsLocator> ItemsLocators { get; set; } = new List<IItemsLocator>();
        public bool IsInitialized { get; private set; }

        public async Task Initialize()
        {
            IsInitialized = true;
        }

        public event EventHandler<ItemLocatorEventArgs> ItemLocatorAddedOrUpdated;
        public event EventHandler<ItemLocatorEventArgs> BeforeItemLocatorDeleted;
        public event EventHandler<ItemLocatorEventArgs> ItemLocatorDeleted;

        public async Task<IEnumerable<IItemsLocator>> GetItemsLocators()
        {
            return ItemsLocators;
        }

        public void Dispose()
        {

        }

        public void AddLocator(IItemsLocator locator)
        {
            ItemsLocators.Add(locator);

            ItemLocatorAddedOrUpdated?.Invoke(this, new ItemLocatorEventArgs { ItemType = locator.ItemType });
        }

        public void RemoveLocator(IItemsLocator locator)
        {
            var ev = new ItemLocatorEventArgs { ItemType = locator.ItemType };

            BeforeItemLocatorDeleted?.Invoke(this, ev);

            ItemsLocators.Remove(locator);

            ItemLocatorDeleted?.Invoke(this, ev);
        }
    }
}