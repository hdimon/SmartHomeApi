using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using ObjectsComparer;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocators;

namespace SmartHomeApi.ItemUtils
{
    public abstract class StandardItemsLocatorAbstract : IStandardItemsLocator
    {
        private readonly AsyncLazy _initializeTask;
        private readonly List<IItem> _items = new List<IItem>();
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        protected readonly ISmartHomeApiFabric Fabric;
        protected readonly IApiLogger Logger;

        public abstract string ItemType { get; }
        public abstract Type ConfigType { get; }
        public virtual bool ImmediateInitialization => false;
        public bool IsInitialized { get; private set; }

        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<ItemEventArgs> ItemDeleted;

        protected StandardItemsLocatorAbstract(ISmartHomeApiFabric fabric)
        {
            Fabric = fabric;
            Logger = fabric.GetApiLogger();

            _initializeTask = new AsyncLazy(InitializeSafely);
        }

        public virtual async Task<IEnumerable<IItem>> GetItems()
        {
            _rwLock.EnterReadLock();

            try
            {
                return _items.ToImmutableList();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public virtual void Dispose()
        {
            foreach (var item in _items)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                    Logger.Info($"Item {item.ItemId} has been disposed.");
                }
            }

            _rwLock.Dispose();
        }

        public virtual async Task ConfigAdded(IItemConfig config)
        {
            if (!IsInitialized)
                return;

            AddOrUpdateItem(config);
        }

        public virtual async Task ConfigUpdated(IItemConfig config)
        {
            if (!IsInitialized)
                return;

            AddOrUpdateItem(config);
        }

        public virtual async Task ConfigDeleted(string itemId)
        {
            if (!IsInitialized)
                return;

            _rwLock.EnterWriteLock();

            try
            {
                var existingItem = _items.FirstOrDefault(i => i.ItemId == itemId);

                if (existingItem == null)
                {
                    Logger.Warning($"Config for Item with id = {itemId} has been deleted but Item already does not exist.");
                    return;
                }

                var itemType = existingItem.ItemType;

                if (existingItem is IDisposable disposable)
                {
                    disposable.Dispose();
                    Logger.Info($"Item {itemId} has been disposed.");
                }

                _items.Remove(existingItem);

                Logger.Info($"Item {itemId} has been deleted.");

                _ = Task.Run(() => ItemDeleted?.Invoke(this, new ItemEventArgs(itemId, itemType)))
                        .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public async Task Initialize()
        {
            await _initializeTask.Value;
        }

        protected abstract IItem ItemFactory(IItemConfig config);

        protected virtual async Task InitializeItem()
        {
            var configs = await Fabric.GetItemsConfigsLocator().GetItemsConfigs(ItemType);

            foreach (var itemConfig in configs)
            {
                AddOrUpdateItem(itemConfig);
            }
        }

        private async Task InitializeSafely()
        {
            if (IsInitialized)
                return;

            try
            {
                await InitializeItem();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            IsInitialized = true;
        }

        private void AddOrUpdateItem(IItemConfig config)
        {
            _rwLock.EnterUpgradeableReadLock();

            try
            {
                var existingItem = _items.FirstOrDefault(i => i.ItemId == config.ItemId);

                if (existingItem == null)
                {
                    try
                    {
                        _rwLock.EnterWriteLock();

                        _items.Add(ItemFactory(config));

                        Task.Run(() => ItemAdded?.Invoke(this, new ItemEventArgs(config.ItemId, config.ItemType)))
                            .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                    finally
                    {
                        _rwLock.ExitWriteLock();
                    }
                }
                else
                {
                    if (!(existingItem is IConfigurable configurableItem))
                        return;

                    if (configurableItem.Config == null)
                    {
                        configurableItem.OnConfigChange(config);
                        return;
                    }

                    var existingConfig = configurableItem.Config;

                    var comparer = new Comparer();
                    IEnumerable<Difference> differences;

                    try
                    {
                        var isEqual = comparer.Compare(ConfigType, existingConfig, config, out differences);

                        if (isEqual)
                            return;
                    }
                    catch (ArgumentException e)
                    {
                        Logger.Warning("Configs comparing error: " + e.Message);

                        //Try to set new config then.
                        configurableItem.OnConfigChange(config);
                        return;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        throw;
                    }

                    Logger.Info($"Config for item {config.ItemId} has been changed.");

                    IEnumerable<ItemConfigChangedField> changedFields =
                        differences?.Select(d => new ItemConfigChangedField { Field = d.MemberPath });

                    configurableItem.OnConfigChange(config, changedFields);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                _rwLock.ExitUpgradeableReadLock();
            }
        }
    }
}