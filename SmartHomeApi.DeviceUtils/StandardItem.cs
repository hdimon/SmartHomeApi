﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Utils;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.DeviceUtils
{
    public abstract class StandardItem : StateChangedSubscriberAbstract, IStateSettable, IStateGettable, IConfigurable, IInitializable
    {
        private readonly AsyncLazy _initializeTask;

        private readonly IItemState _defaultState;
        public string ItemId { get; }
        public string ItemType { get; }
        public IItemConfig Config { get; private set; }
        public bool IsInitialized { get; set; }

        public virtual IList<string> UncachedFields { get; }
        public virtual IList<string> UntrackedFields { get; }

        protected StandardItem(IApiManager manager, IItemHelpersFabric helpersFabric, IItemConfig config) : base(manager, helpersFabric)
        {
            Config = config;

            ItemId = config.ItemId;
            ItemType = config.ItemType;

            _defaultState = new ItemState(ItemId, ItemType) { ConnectionStatus = ConnectionStatus.Stable };

            _initializeTask = new AsyncLazy(InitializeSafely);
        }

        public void OnConfigChange(IItemConfig newConfig, IEnumerable<ItemConfigChangedField> changedFields = null)
        {
            Config = newConfig;
        }

        public virtual async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            return new SetValueResult();
        }

        public virtual IItemState GetState()
        {
            return _defaultState;
        }


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
                await InitializeDevice();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            IsInitialized = true;
        }

        protected virtual async Task InitializeDevice()
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            
        }
    }
}