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
        private readonly IStandardItemsLocator _standardItemsLocator;

        protected readonly IItemsLocator _locator;

        public virtual string ItemType => _locator.ItemType;
        public virtual Type ConfigType => _locator.ConfigType;
        public virtual bool ImmediateInitialization => _locator.ImmediateInitialization;
        public virtual bool IsInitialized => _standardItemsLocator.IsInitialized;

        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<ItemEventArgs> ItemDeleted;

        public StandardItemsLocatorBridge(IItemsLocator locator)
        {
            _locator = locator;

            _standardItemsLocator = _locator as IStandardItemsLocator;

            if (_standardItemsLocator != null)
            {
                _standardItemsLocator.ItemAdded += StandardItemsLocatorOnItemAdded;
                _standardItemsLocator.ItemDeleted += StandardItemsLocatorOnItemDeleted;
            }
        }

        public virtual async Task Initialize()
        {
            await _standardItemsLocator.Initialize();
        }

        public virtual async Task<IEnumerable<IItem>> GetItems()
        {
            return await _locator.GetItems();
        }

        public virtual async Task ConfigAdded(IItemConfig config)
        {
            await _standardItemsLocator.ConfigAdded(config);
        }

        public virtual async Task ConfigUpdated(IItemConfig config)
        {
            await _standardItemsLocator.ConfigUpdated(config);
        }

        public virtual async Task ConfigDeleted(string itemId)
        {
            await _standardItemsLocator.ConfigDeleted(itemId);
        }

        public virtual void Dispose()
        {
            if (_standardItemsLocator != null)
            {
                _standardItemsLocator.ItemAdded -= StandardItemsLocatorOnItemAdded;
                _standardItemsLocator.ItemDeleted -= StandardItemsLocatorOnItemDeleted;
            }

            _locator.Dispose();
        }

        private void StandardItemsLocatorOnItemAdded(object sender, ItemEventArgs e)
        {
            Task.Run(() => ItemAdded?.Invoke(this, e));
        }

        private void StandardItemsLocatorOnItemDeleted(object sender, ItemEventArgs e)
        {
            Task.Run(() => ItemDeleted?.Invoke(this, e));
        }
    }
}