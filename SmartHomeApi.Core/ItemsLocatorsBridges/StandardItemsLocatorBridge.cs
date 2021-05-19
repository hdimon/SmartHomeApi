using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocators;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;

[assembly: InternalsVisibleTo("SmartHomeApi.Core.UnitTests")]
namespace SmartHomeApi.Core.ItemsLocatorsBridges
{
    class StandardItemsLocatorBridge : IStandardItemsLocatorBridge
    {
        protected readonly IItemsLocator _locator;

        public virtual string ItemType => _locator.ItemType;
        public virtual Type ConfigType => _locator.ConfigType;
        public virtual bool ImmediateInitialization => _locator.ImmediateInitialization;

        public StandardItemsLocatorBridge(IItemsLocator locator)
        {
            _locator = locator;
        }

        public virtual async Task<IEnumerable<IItem>> GetItems()
        {
            return await _locator.GetItems();
        }

        public virtual async Task ConfigAdded(IItemConfig config)
        {
            await ((IStandardItemsLocator)_locator).ConfigAdded(config);
        }

        public virtual async Task ConfigUpdated(IItemConfig config)
        {
            await ((IStandardItemsLocator)_locator).ConfigUpdated(config);
        }

        public virtual async Task ConfigDeleted(string itemId)
        {
            await ((IStandardItemsLocator)_locator).ConfigDeleted(itemId);
        }

        public virtual void Dispose()
        {
            _locator.Dispose();
        }
    }
}