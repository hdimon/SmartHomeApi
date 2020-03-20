using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Models;

namespace SmartHomeApi.Core.Services
{
    public class ApiManager : IApiManager
    {
        private readonly ISmartHomeApiFabric _fabric;
        private ApiManagerStateContainer _stateContainer;
        private Task _worker;
        private readonly ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private readonly IApiLogger _logger;
        private readonly IStatesContainerTransformer _stateContainerTransformer;
        private readonly INotificationsProcessor _notificationsProcessor;
        private readonly IUntrackedStatesProcessor _untrackedStatesProcessor;
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        public string ItemType => null;
        public string ItemId => null;

        public ApiManager(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();
            _stateContainerTransformer = _fabric.GetStateContainerTransformer();
            _notificationsProcessor = _fabric.GetNotificationsProcessor();
            _untrackedStatesProcessor = _fabric.GetUntrackedStatesProcessor();

            var state = CreateStatesContainer();
            _stateContainer = new ApiManagerStateContainer(state);

            
        }

        public bool IsInitialized { get; private set; }

        public async Task Initialize()
        {
            _logger.Info("Starting ApiManager initialization...");

            await _fabric.GetItemsConfigsLocator().Initialize();

            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();
            var immediateItems = locators.Where(l => l.ImmediateInitialization).ToList();

            _logger.Info("Running items with immediate initialization...");

            await Task.WhenAll(immediateItems.Select(GetItems));

            _logger.Info("Items with immediate initialization have been run.");

            RunStatesCollectorWorker();

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

        public void Dispose()
        {
            _logger.Info("Disposing ApiManager...");

            _disposingCancellationTokenSource.Cancel();

            var pluginsLocator = _fabric.GetItemsPluginsLocator();
            var configLocator = _fabric.GetItemsConfigsLocator();

            pluginsLocator.Dispose();

            configLocator.Dispose();

            _logger.Info("ApiManager has been disposed.");
        }

        public async Task<ISetValueResult> SetValue(string deviceId, string parameter, string value)
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            var item = items.FirstOrDefault(i => i is IStateSettable it && it.ItemId == deviceId);

            if (item == null)
                return new SetValueResult(false);

            var not = (IStateSettable)item;

            var currentPatameterState = await GetState(not.ItemId, parameter);

            var ev = new StateChangedEvent(StateChangedEventType.ValueSet, not.ItemType, not.ItemId, parameter,
                currentPatameterState?.ToString(), value);

            _stateContainerTransformer.AddStateChangedEvent(ev);

            _notificationsProcessor.NotifySubscribers(ev);

            ISetValueResult result = null;

            try
            {
                result = await not.SetValue(parameter, value);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            _stateContainerTransformer.RemoveStateChangedEvent(ev);

            return result ?? new SetValueResult(false);
        }

        public async Task<ISetValueResult> Increase(string deviceId, string parameter)
        {
            return new SetValueResult(false);
        }

        public async Task<ISetValueResult> Decrease(string deviceId, string parameter)
        {
            return new SetValueResult(false);
        }

        public async Task<IStatesContainer> GetState(bool transform = false)
        {
            var stateContainer = GetStateSafely();

            var state = await TransformStateIfRequired(stateContainer.State, transform);

            state = (IStatesContainer)state.Clone();

            var trState = new Dictionary<string, IItemState>();

            foreach (var itemStatePair in state.States)
            {
                var st = await FilterOutUncachedStates(itemStatePair.Value, stateContainer);

                if (st != null)
                    trState.Add(itemStatePair.Key, st);
            }

            state.States = trState;

            return state;
        }

        public async Task<IItemState> GetState(string itemId, bool transform = false)
        {
            var stateContainer = GetStateSafely();

            var state = await TransformStateIfRequired(stateContainer.State, transform);

            if (!state.States.ContainsKey(itemId))
                return new ItemState(itemId, string.Empty);

            var itemState = state.States[itemId];

            itemState = await FilterOutUncachedStates(itemState, stateContainer);

            if (itemState == null)
                return new ItemState(itemId, string.Empty);

            return itemState;

        }

        public async Task<object> GetState(string deviceId, string parameter, bool transform = false)
        {
            var stateContainer = GetStateSafely();

            var state = await TransformStateIfRequired(stateContainer.State, transform);

            if (state.States.ContainsKey(deviceId))
            {
                var deviceState = state.States[deviceId];

                if (deviceState.States.ContainsKey(parameter))
                    return deviceState.States[parameter];
            }

            return string.Empty;
        }

        private async Task<IItemState> FilterOutUncachedStates(IItemState itemState,
            ApiManagerStateContainer stateContainer)
        {
            if (!stateContainer.UncachedStates.ContainsKey(itemState.ItemId))
                return itemState;

            var state = (IItemState)itemState.Clone();

            var uncachedState = stateContainer.UncachedStates[itemState.ItemId];

            //If ApplyOnlyEnumeratedStates = false then whole Item in uncached
            if (!uncachedState.ApplyOnlyEnumeratedStates)
                return null;

            var cachedStates = new Dictionary<string, object>();

            foreach (var stateState in state.States)
            {
                if (uncachedState.States.Contains(stateState.Key))
                    continue;

                cachedStates.Add(stateState.Key, stateState.Value);
            }

            state.States = cachedStates;

            return state;
        }

        public async Task<IList<IItem>> GetItems()
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            return items.ToList();
        }

        private IStatesContainer CreateStatesContainer()
        {
            return new DeviceStatesContainer();
        }

        private ApiManagerStateContainer GetStateSafely()
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            ApiManagerStateContainer stateContainer = _stateContainer;

            _readerWriterLock.ReleaseReaderLock();

            return stateContainer;
        }

        private void RunStatesCollectorWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(CollectItemsStates).Unwrap().ContinueWith(
                    t => { _logger.Error(t.Exception); },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task CollectItemsStates()
        {
            while (!_disposingCancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(250, _disposingCancellationTokenSource.Token);

                try
                {
                    var state = CreateStatesContainer();
                    var stateContainer = new ApiManagerStateContainer(state);

                    var gettableItems = await GetGettableItems();
                    _untrackedStatesProcessor.AddUntrackedItemsFromConfig(stateContainer);
                    AddUncachedItemsFromConfig(stateContainer);

                    foreach (var item in gettableItems)
                    {
                        CollectItemState(item, stateContainer);
                    }

                    var oldStateContainer = GetStateSafely();
                    SetStateSafely(stateContainer);

                    _notificationsProcessor.NotifySubscribersAboutChanges(oldStateContainer, _stateContainer);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error when collecting items states.");
                }
            }
        }

        private void CollectItemState(IStateGettable item, ApiManagerStateContainer stateContainer)
        {
            IStatesContainer state = stateContainer.State;

            try
            {
                var deviceState = item.GetState();

                state.States.Add(deviceState.ItemId, deviceState);

                _untrackedStatesProcessor.AddUntrackedStatesFromItem(item, stateContainer);
                AddUncachedStatesFromItem(item, stateContainer);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when collecting items states.");
            }
        }

        private static void AddUncachedStatesFromItem(IStateGettable item, ApiManagerStateContainer stateContainer)
        {
            if (item.UncachedFields == null || !item.UncachedFields.Any())
                return;

            if (stateContainer.UncachedStates.ContainsKey(item.ItemId))
            {
                var uncachedItem = stateContainer.UncachedStates[item.ItemId];

                //If null then whole Item is uncached if not null then merge states
                if (uncachedItem.ApplyOnlyEnumeratedStates)
                {
                    foreach (var itemUncachedField in item.UncachedFields)
                    {
                        if (!uncachedItem.States.Contains(itemUncachedField))
                            uncachedItem.States.Add(itemUncachedField);
                    }
                }
            }
            else
                stateContainer.UncachedStates.Add(item.ItemId,
                    new AppSettingItemInfo
                    {
                        ItemId = item.ItemId,
                        ApplyOnlyEnumeratedStates = true,
                        States = item.UncachedFields.ToList()
                    });
        }

        private void AddUncachedItemsFromConfig(ApiManagerStateContainer stateContainer)
        {
            var uncachedItems = _fabric.GetConfiguration().UncachedItems;

            foreach (var uncachedItem in uncachedItems)
            {
                if (string.IsNullOrWhiteSpace(uncachedItem.ItemId) ||
                    stateContainer.UncachedStates.ContainsKey(uncachedItem.ItemId))
                    continue;

                if (uncachedItem.ApplyOnlyEnumeratedStates && uncachedItem.States == null)
                    uncachedItem.States = new List<string>();

                stateContainer.UncachedStates.Add(uncachedItem.ItemId, uncachedItem);
            }
        }

        private void SetStateSafely(ApiManagerStateContainer stateContainer)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

                _stateContainer = stateContainer;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when setting API state.");
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        private async Task<List<IStateTransformable>> GetTransformableItems()
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            var transformableItems = items.Where(d => d is IStateTransformable).Cast<IStateTransformable>().ToList();

            return transformableItems;
        }

        private async Task<List<IStateGettable>> GetGettableItems()
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            var gettableItems = items.Where(d => d is IStateGettable).Cast<IStateGettable>()
                                     .OrderBy(i => i.ItemType).ThenBy(i => i.ItemId).ToList();

            return gettableItems;
        }

        private async Task<IStatesContainer> TransformStateIfRequired(IStatesContainer state, bool transform)
        {
            if (!transform)
                return state;

            var transformableItems = await GetTransformableItems();

            if (transformableItems.Any() || _stateContainerTransformer.TransformationIsNeeded())
            {
                state = (IStatesContainer)state.Clone();
                _stateContainerTransformer.Transform(state, transformableItems);
            }

            return state;
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