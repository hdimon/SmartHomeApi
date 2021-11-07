﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocators;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests.Stubs.ApiItemsLocatorTestStubs
{
    class ItemsLocatorStub1 : IStandardItemsLocator
    {
        private readonly List<StandardItem> _items = new List<StandardItem>();

        protected readonly ISmartHomeApiFabric Fabric;
        protected readonly IApiLogger Logger;

        public string ItemType => nameof(ItemsLocatorStub1);
        public Type ConfigType { get; }
        public bool ImmediateInitialization => false;

        public ItemsLocatorStub1(ISmartHomeApiFabric fabric)
        {
            Fabric = fabric;
            Logger = fabric.GetApiLogger();
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            return _items;
        }

        public bool IsInitialized { get; }

        public async Task Initialize()
        {
        }

        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<ItemEventArgs> ItemDeleted;

        public async Task ConfigAdded(IItemConfig config)
        {
            var item = new TestItem(Fabric.GetApiManager(), Fabric.GetItemHelpersFabric(config.ItemId, config.ItemType), config);

            _items.Add(item);

            _ = Task.Run(() => ItemAdded?.Invoke(this, new ItemEventArgs(config.ItemId, config.ItemType)))
                    .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public Task ConfigUpdated(IItemConfig config)
        {
            throw new NotImplementedException();
        }

        public async Task ConfigDeleted(string itemId)
        {
            var item = _items.First(i => i.ItemId == itemId);
            _items.Remove(item);

            _ = Task.Run(() => ItemDeleted?.Invoke(this, new ItemEventArgs(item.ItemId, item.ItemType)))
                    .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Dispose()
        {
        }
    }
}