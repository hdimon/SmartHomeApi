using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.ItemUtils
{
    public abstract class StandardItem : StateChangedSubscriberAbstract, IStateSettable, IStateGettable, IConfigurable, IInitializable, IExecutable
    {
        private readonly AsyncLazy _initializeTask;

        protected IItemState State;

        public override string ItemId { get; }
        public override string ItemType { get; }

        public IItemConfig Config { get; private set; }
        public bool IsInitialized { get; set; }

        protected StandardItem(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager, helpersFabric)
        {
            Config = config;

            ItemId = config.ItemId;
            ItemType = config.ItemType;

            _initializeTask = new AsyncLazy(InitializeSafely);
        }

        public virtual void OnConfigChange(IItemConfig newConfig, IEnumerable<ItemConfigChangedField> changedFields = null)
        {
            Config = newConfig;
        }

        public virtual Task<ISetValueResult> SetValue(string parameter, object value)
        {
            return Task.FromResult<ISetValueResult>(new SetValueResult());
        }

        public virtual Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command)
        {
            return Task.FromResult<ExecuteCommandResultAbstract>(new ExecuteCommandResultNotFound());
        }

        public async Task Initialize()
        {
            await _initializeTask.Value;
        }

        protected virtual Task InitializeItem()
        {
            return Task.CompletedTask;
        }

        protected override Task ProcessNotification(StateChangedEvent args)
        {
            return Task.CompletedTask;
        }

        protected void SubscribeOnNotifications()
        {
            Manager.RegisterSubscriber(this);
            Logger.Info("Subscribed on notifications.");
        }

        protected void CreateItemState()
        {
            State = (IItemState)HelpersFabric.GetOrCreateItemState();
            Logger.Info("Got or created item state.");
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
    }
}