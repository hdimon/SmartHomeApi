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

        public string ItemId { get; }
        public string ItemType { get; }

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

        public virtual async Task<ISetValueResult> SetValue(string parameter, object value)
        {
            return new SetValueResult();
        }

        public virtual async Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command)
        {
            return new ExecuteCommandResultNotFound();
        }

        public virtual IItemState GetState()
        {
            return null;
        }

        public async Task Initialize()
        {
            await _initializeTask.Value;
        }

        protected virtual async Task InitializeItem()
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {

        }

        protected void SubscribeOnNotifications()
        {
            Manager.RegisterSubscriber(this);
            Logger.Info("Subscribed on notifications.");
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