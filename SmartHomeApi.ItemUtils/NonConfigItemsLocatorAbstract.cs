using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocators;

namespace SmartHomeApi.ItemUtils
{
    public abstract class NonConfigItemsLocatorAbstract : IStandardItemsLocator
    {
        private readonly AsyncLazy _initializeTask;
        private ImmutableList<IItem> _items = new List<IItem>().ToImmutableList();

        protected readonly ISmartHomeApiFabric Fabric;
        protected readonly IApiLogger Logger;

        public abstract string ItemType { get; }
        public Type ConfigType => null; //NonConfigItemsLocator does not have config
        public bool ImmediateInitialization => true;
        public bool IsInitialized { get; private set; }

        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<ItemEventArgs> ItemDeleted;

        protected NonConfigItemsLocatorAbstract(ISmartHomeApiFabric fabric)
        {
            Fabric = fabric;
            Logger = fabric.GetApiLogger();

            _initializeTask = new AsyncLazy(InitializeSafely);
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            return _items;
        }

        public async Task Initialize()
        {
            await _initializeTask.Value;
        }


        public async Task ConfigAdded(IItemConfig config)
        {
            //NonConfigItemsLocator does not have config so should not accept it even if config has been created
        }

        public async Task ConfigUpdated(IItemConfig config)
        {
            //NonConfigItemsLocator does not have config so should not accept it even if config has been created
        }

        public async Task ConfigDeleted(string itemId)
        {
            //NonConfigItemsLocator does not have config so should not accept it even if config has been created
        }

        public void Dispose()
        {
            try
            {
                foreach (var item in _items)
                {
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                        Fabric.GetApiLogger().Info($"Item {item.ItemId} has been disposed.");
                    }
                }

                _items = new List<IItem>().ToImmutableList();
            }
            catch (Exception e)
            {
                Fabric.GetApiLogger().Error(e);
            }
        }

        protected abstract Task<IList<IItem>> CreateItems();

        private async Task InitializeSafely()
        {
            if (IsInitialized)
                return;

            try
            {
                var items = await CreateItems();

                if (items != null)
                {
                    _items = items.ToImmutableList();

                    foreach (var item in _items)
                    {
                        _ = Task.Run(() => ItemAdded?.Invoke(this, new ItemEventArgs(item.ItemId, item.ItemType)))
                                .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            IsInitialized = true;
        }
    }
}