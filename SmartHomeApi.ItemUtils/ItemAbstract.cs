using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public abstract class ItemAbstract : IItem, IStateSettable, IStateGettable, IConfigurable, IInitializable, IDisposable
    {
        private readonly AsyncLazy _initializeTask;

        protected readonly IItemHelpersFabric HelpersFabric;
        protected readonly IApiLogger Logger;
        protected CancellationTokenSource DisposingCancellationTokenSource = new CancellationTokenSource();
        
        public string ItemId { get; }
        public string ItemType { get; }
        public IItemConfig Config { get; private set; }

        public virtual void OnConfigChange(IItemConfig newConfig, IEnumerable<ItemConfigChangedField> changedFields)
        {
            Config = newConfig;
        }

        protected ItemAbstract(IItemHelpersFabric helpersFabric, IItemConfig config)
        {
            HelpersFabric = helpersFabric;
            Config = config;
            Logger = HelpersFabric.GetApiLogger();

            ItemId = config.ItemId;
            ItemType = config.ItemType;

            _initializeTask = new AsyncLazy(InitializeSafely);
        }

        public abstract Task<ISetValueResult> SetValue(string parameter, object value);
        public abstract IItemState GetState();

        public bool IsInitialized { get; set; }

        public async Task Initialize()
        {
            await _initializeTask.Value;
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

        protected virtual async Task InitializeItem()
        {
        }

        public virtual void Dispose()
        {
            try
            {
                DisposingCancellationTokenSource.Cancel();
                Logger.Info($"Item {ItemId} has been disposed.");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}