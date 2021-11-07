using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.Core.Services
{
    public class ApiManager : IApiManager
    {
        private bool _disposed;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IItemStatesProcessor _statesProcessor;
        private readonly IApiLogger _logger;
        private readonly IApiItemsLocator _apiItemsLocator;
        private readonly INotificationsProcessor _notificationsProcessor;
        private readonly IUncachedStatesProcessor _uncachedStatesProcessor;
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        public string ItemType => null;
        public string ItemId => null;
        public bool IsInitialized { get; private set; }

        public ApiManager(ISmartHomeApiFabric fabric, IItemStatesProcessor statesProcessor)
        {
            _fabric = fabric;
            _statesProcessor = statesProcessor;
            _logger = _fabric.GetApiLogger();
            _apiItemsLocator = _fabric.GetApiItemsLocator();
            _notificationsProcessor = _fabric.GetNotificationsProcessor();
            _uncachedStatesProcessor = _fabric.GetUncachedStatesProcessor();
        }

        public async Task Initialize()
        {
            _logger.Info("Starting ApiManager initialization...");

            //First collect all initial plugins and then item configs
            await _fabric.GetItemsPluginsLocator().Initialize();
            await _fabric.GetItemsConfigsLocator().Initialize();
            await _apiItemsLocator.Initialize();

            IsInitialized = true;

            _logger.Info("ApiManager initialized.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _logger.Info("Disposing ApiManager...");

            _disposingCancellationTokenSource.Cancel();

            var notificationsProcessor = _fabric.GetNotificationsProcessor();
            var pluginsLocator = _fabric.GetItemsPluginsLocator();
            var configLocator = _fabric.GetItemsConfigsLocator();

            await _apiItemsLocator.DisposeAsync();
            await notificationsProcessor.DisposeAsync();
            pluginsLocator.Dispose();
            configLocator.Dispose();

            _logger.Info("ApiManager has been disposed.");

            _disposed = true;
        }

        public async Task<ISetValueResult> SetValue(string itemId, string parameter, object value)
        {
            var items = await _apiItemsLocator.GetItems();

            var item = items.FirstOrDefault(i => i is IStateSettable it && it.ItemId == itemId);

            if (item == null)
                return new SetValueResult(itemId, null, false);

            var not = (IStateSettable)item;

            var currentParameterState = await GetState(not.ItemId, parameter);

            var ev = new StateChangedEvent(StateChangedEventType.ValueSet, not.ItemType, not.ItemId, parameter,
                currentParameterState, value);

            _notificationsProcessor.NotifySubscribers(ev);

            ISetValueResult result = null;

            try
            {
                result = await not.SetValue(parameter, value);

                result = new SetValueResult(not.ItemId, not.ItemType, result.Success);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return result ?? new SetValueResult(not.ItemId, not.ItemType, false);
        }

        public async Task<ISetValueResult> Increase(string itemId, string parameter)
        {
            return new SetValueResult(false);
        }

        public async Task<ISetValueResult> Decrease(string itemId, string parameter)
        {
            return new SetValueResult(false);
        }

        public async Task<IStatesContainer> GetState()
        {
            var state = _statesProcessor.GetStatesContainer();

            state = _uncachedStatesProcessor.FilterOutUncachedStates(state);

            return await Task.FromResult(state);
        }

        public async Task<IItemState> GetState(string itemId)
        {
            var state = _statesProcessor.GetStatesContainer();

            if (!state.States.ContainsKey(itemId))
                return null;

            var itemState = state.States[itemId];

            return await Task.FromResult(itemState);
        }

        public async Task<object> GetState(string itemId, string parameter)
        {
            var state = _statesProcessor.GetStatesContainer();

            if (!state.States.ContainsKey(itemId)) 
                return null;

            var itemState = state.States[itemId];

            if (itemState.States.ContainsKey(parameter))
                return await Task.FromResult(itemState.States[parameter]);

            return null;
        }

        public async Task<IList<IItem>> GetItems()
        {
            var items = await _apiItemsLocator.GetItems();

            return items.ToList();
        }

        public async Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command)
        {
            var items = await _apiItemsLocator.GetItems();

            var item = items.FirstOrDefault(i => i is IExecutable it && it.ItemId == command.ItemId);

            if (item == null)
                return new ExecuteCommandResultNotFound();

            var eItem = (IExecutable)item;

            ExecuteCommandResultAbstract result = null;

            try
            {
                result = await eItem.Execute(command);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return result ?? new ExecuteCommandResultInternalError();
        }

        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _notificationsProcessor.RegisterSubscriber(subscriber);
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _notificationsProcessor.UnregisterSubscriber(subscriber);
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
            _notificationsProcessor.NotifySubscribers(args);
        }
    }
}