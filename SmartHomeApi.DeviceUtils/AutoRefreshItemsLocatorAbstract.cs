using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.DeviceUtils
{
    public abstract class AutoRefreshItemsLocatorAbstract : IItemsLocator
    {
        private Task _worker;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private bool _isFirstRun = true;

        protected readonly ISmartHomeApiFabric Fabric;
        protected readonly IApiLogger Logger;
        protected Dictionary<string, IItem> Items = new Dictionary<string, IItem>();
        protected CancellationTokenSource DisposingCancellationTokenSource = new CancellationTokenSource();

        public abstract string ItemType { get; }
        public abstract Type ConfigType { get; }
        public virtual bool ImmediateInitialization => false;

        protected AutoRefreshItemsLocatorAbstract(ISmartHomeApiFabric fabric)
        {
            Fabric = fabric;
            Logger = fabric.GetApiLogger();

            RunItemsLocatorWorker();
        }

        private void RunItemsLocatorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(ItemsLocatorWorkerWrapper).Unwrap().ContinueWith(
                    t => { Logger.Error(t.Exception); },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task ItemsLocatorWorkerWrapper()
        {
            while (!DisposingCancellationTokenSource.IsCancellationRequested)
            {
                if (!_isFirstRun)
                    await Task.Delay(1000, DisposingCancellationTokenSource.Token);

                try
                {
                    await ItemsLocatorWorker();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private async Task ItemsLocatorWorker()
        {
            var configLocator = Fabric.GetItemsConfigsLocator();

            var configs = await configLocator.GetItemsConfigs(ItemType);

            var items = await UpdateItemsList(configs);

            Items = items;

            if (_isFirstRun)
            {
                _taskCompletionSource.SetResult(true);
                _isFirstRun = false;
            }
        }

        protected abstract IItem ItemFactory(IItemConfig config);

        protected async Task<Dictionary<string, IItem>> UpdateItemsList(IList<IItemConfig> configs)
        {
            var items = new Dictionary<string, IItem>();

            DeleteItems(configs);

            foreach (var config in configs)
            {
                if (Items.ContainsKey(config.ItemId))
                {
                    items.Add(config.ItemId, Items[config.ItemId]);
                    continue; //Update config
                }

                items.Add(config.ItemId, ItemFactory(config));
                Logger.Info($"Item {config.ItemId} (type: {config.ItemType}) has been created.");
            }

            return items;
        }

        protected void DeleteItems(IList<IItemConfig> configs)
        {
            var deletedItemIds = Items.Keys.Except(configs.Select(c => c.ItemId)).ToList();

            foreach (var deletedItemId in deletedItemIds)
            {
                try
                {
                    if (Items.TryGetValue(deletedItemId, out var deleted))
                    {
                        if (deleted is IDisposable disposable)
                            disposable.Dispose();

                        Logger.Info($"Item {deletedItemId} has been deleted.");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        public async Task<IEnumerable<IItem>> GetItems()
        {
            if (_isFirstRun)
                await _taskCompletionSource.Task;

            return Items.Values;
        }

        public virtual void Dispose()
        {
            try
            {
                DisposingCancellationTokenSource.Cancel();
                //Logger.Info($"ItemsLocator {ItemType} has been disposed.");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}