using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;

namespace SmartHomeApi.Core.UnitTests.Stubs
{
    class ItemsPluginsLocatorForConfigTests : IItemsPluginsLocator
    {
        public List<IStandardItemsLocatorBridge> ItemsLocators { get; set; } = new List<IStandardItemsLocatorBridge>();
        public bool IsInitialized { get; private set; }

        public Task Initialize()
        {
            IsInitialized = true;

            return Task.CompletedTask;
        }

        public event EventHandler<ItemLocatorEventArgs> ItemLocatorAddedOrUpdated;
        public event EventHandler<ItemLocatorEventArgs> BeforeItemLocatorDeleted;
        public event EventHandler<ItemLocatorEventArgs> ItemLocatorDeleted;

        public Task<IEnumerable<IStandardItemsLocatorBridge>> GetItemsLocators()
        {
            return Task.FromResult<IEnumerable<IStandardItemsLocatorBridge>>(ItemsLocators);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void AddLocator(IStandardItemsLocatorBridge locator)
        {
            var existingLocator = ItemsLocators.FirstOrDefault(p => p.ItemType == locator.ItemType);

            if (existingLocator != null)
            {
                BeforeItemLocatorDeleted?.Invoke(this, new ItemLocatorEventArgs { ItemType = existingLocator.ItemType });
                ItemsLocators.Remove(existingLocator);
            }

            ItemsLocators.Add(locator);

            ItemLocatorAddedOrUpdated?.Invoke(this, new ItemLocatorEventArgs { ItemType = locator.ItemType });
        }

        public void RemoveLocator(IStandardItemsLocatorBridge locator)
        {
            var ev = new ItemLocatorEventArgs { ItemType = locator.ItemType };

            BeforeItemLocatorDeleted?.Invoke(this, ev);

            ItemsLocators.Remove(locator);

            ItemLocatorDeleted?.Invoke(this, ev);
        }
    }
}