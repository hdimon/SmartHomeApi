using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocators;

namespace SmartHomeApi.ItemUtils
{
    public abstract class StandardItemsLocatorAbstract : IStandardItemsLocator
    {
        public abstract string ItemType { get; }
        public abstract Type ConfigType { get; }
        public virtual bool ImmediateInitialization => false;

        public virtual async Task<IEnumerable<IItem>> GetItems()
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual async Task ConfigAdded(IItemConfig config)
        {
        }

        public virtual async Task ConfigUpdated(IItemConfig config)
        {
        }

        public virtual async Task ConfigDeleted(string itemId)
        {
        }
    }
}