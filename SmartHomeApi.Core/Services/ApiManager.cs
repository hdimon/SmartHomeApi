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
            _notificationsProcessor = _fabric.GetNotificationsProcessor();
            _uncachedStatesProcessor = _fabric.GetUncachedStatesProcessor();
        }

        public async Task Initialize()
        {
            _logger.Info("Starting ApiManager initialization...");

            //First collect all initial plugins and then item configs
            await _fabric.GetItemsPluginsLocator().Initialize();
            await _fabric.GetItemsConfigsLocator().Initialize();

            //
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();

            //Temp
            foreach (var itemsLocator in locators)
            {
                await itemsLocator.Initialize();
            }
            //Temp

            //TODO Instead of ImmediateInitialization introduce InitialLoadPriority setting in Item config, group items by this and initialize them in groups

            var immediateItems = locators.Where(l => l.ImmediateInitialization).ToList();

            _logger.Info("Running items with immediate initialization...");

            await Task.WhenAll(immediateItems.Select(GetItems));

            _logger.Info("Items with immediate initialization have been run.");
            //

            IsInitialized = true;

            _logger.Info("ApiManager initialized.");
        }

        private async Task<IEnumerable<IItem>> GetItems(IItemsLocator locator)
        {
            var items = await locator.GetItems();
            var itemsList = items.ToList();

            await Task.WhenAll(itemsList
                               .Where(item => item is IInitializable initializable && !initializable.IsInitialized)
                               .Select(item => ((IInitializable)item).Initialize()));

            return itemsList;
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

            await notificationsProcessor.DisposeAsync();
            pluginsLocator.Dispose();
            configLocator.Dispose();

            _logger.Info("ApiManager has been disposed.");

            _disposed = true;
        }

        public async Task<ISetValueResult> SetValue(string itemId, string parameter, object value)
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

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

            return state;
        }

        public async Task<IItemState> GetState(string itemId)
        {
            var state = _statesProcessor.GetStatesContainer();

            if (!state.States.ContainsKey(itemId))
                return null;

            var itemState = state.States[itemId];

            return itemState;
        }

        public async Task<object> GetState(string itemId, string parameter)
        {
            var state = _statesProcessor.GetStatesContainer();

            if (!state.States.ContainsKey(itemId)) 
                return null;

            var itemState = state.States[itemId];

            if (itemState.States.ContainsKey(parameter))
                return itemState.States[parameter];

            return null;
        }

        public async Task<IList<IItem>> GetItems()
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            return items.ToList();
        }

        public async Task<ExecuteCommandResultAbstract> Execute(ExecuteCommand command)
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

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