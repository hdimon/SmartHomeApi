﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Models;

namespace SmartHomeApi.Core.Services
{
    public class ApiManager : IApiManager
    {
        private readonly ISmartHomeApiFabric _fabric;
        private ApiManagerStateContainer _stateContainer;
        private Task _worker;
        private readonly ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private readonly List<IStateChangedSubscriber> _stateChangedSubscribers = new List<IStateChangedSubscriber>();
        private readonly IApiLogger _logger;
        private readonly IStatesContainerTransformer _stateContainerTransformer;
        private readonly CancellationTokenSource _disposingCancellationTokenSource = new CancellationTokenSource();

        public string ItemType => null;
        public string ItemId => null;

        public ApiManager(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();

            var state = CreateStatesContainer();
            _stateContainer = new ApiManagerStateContainer(state);

            _stateContainerTransformer = _fabric.GetStateContainerTransformer();
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

            NotifySubscribers(ev);

            var result = await not.SetValue(parameter, value);

            _stateContainerTransformer.RemoveStateChangedEvent(ev);

            return result;
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
                var st = await FilterOutUntrackableStates(itemStatePair.Value);
                trState.Add(itemStatePair.Key, st);
            }

            state.States = trState;

            return state;
        }

        public async Task<IItemState> GetState(string itemId, bool transform = false)
        {
            var stateContainer = GetStateSafely();

            var state = await TransformStateIfRequired(stateContainer.State, transform);

            if (state.States.ContainsKey(itemId))
            {
                var itemState = state.States[itemId];

                itemState = await FilterOutUntrackableStates(itemState);

                return itemState;
            }

            return new ItemState(itemId, string.Empty);
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

        private async Task<IItemState> FilterOutUntrackableStates(IItemState itemState)
        {
            var gettableItems = await GetGettableItems();
            var gettableItem = gettableItems.FirstOrDefault(i => i.ItemId == itemState.ItemId);

            if (gettableItem == null)
                return itemState;

            var state = (IItemState)itemState.Clone();
            var trackedStates = GetOnlyTrackedStates(gettableItem, state.States);
            state.States = trackedStates;

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
                    t =>
                    {
                        _logger.Error(t.Exception);
                    },
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

                    foreach (var item in gettableItems)
                    {
                        try
                        {
                            var deviceState = item.GetState();

                            state.States.Add(deviceState.ItemId, deviceState);

                            if (item.UntrackedFields != null && item.UntrackedFields.Any())
                                stateContainer.UntrackedStates.Add(item.ItemId, item.UntrackedFields);

                            if (item.UncachedFields != null && item.UncachedFields.Any())
                                stateContainer.UncachedStates.Add(item.ItemId, item.UncachedFields);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Error when collecting items states.");
                        }
                    }

                    var oldState = await GetState();
                    var oldStateContainer = GetStateSafely();
                    oldStateContainer.State = oldState;
                    SetStateSafely(stateContainer);

                    NotifySubscribersAboutChanges(oldStateContainer, gettableItems);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error when collecting items states.");
                }
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

        private void NotifySubscribersAboutChanges(ApiManagerStateContainer oldStateContainer, List<IStateGettable> gettableItems)
        {
            var newStates = _stateContainer.State.States;
            var oldStates = oldStateContainer.State.States;

            var addedDevices = newStates.Keys.Except(oldStates.Keys).ToList();
            var removedDevices = oldStates.Keys.Except(newStates.Keys).ToList();
            var updatedDevices = newStates.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(removedDevices, oldStateContainer);
            NotifySubscribersAboutAddedDevices(addedDevices, _stateContainer);
            NotifySubscribersAboutUpdatedDevices(updatedDevices, oldStateContainer, _stateContainer);
        }

        private void NotifySubscribersAboutRemovedDevices(List<string> removedDevices,
            ApiManagerStateContainer oldStateContainer)
        {
            foreach (var removedDevice in removedDevices)
            {
                var itemState = oldStateContainer.State.States[removedDevice];

                var trackedStates = GetOnlyTrackedStates(oldStateContainer.UntrackedStates, itemState);

                foreach (var telemetryPair in trackedStates)
                {
                    var valueString = GetValueString(telemetryPair.Value);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, itemState.ItemType,
                        itemState.ItemId, telemetryPair.Key, valueString, null));
                }
            }
        }

        private void NotifySubscribersAboutAddedDevices(List<string> addedDevices,
            ApiManagerStateContainer newStateContainer)
        {
            foreach (var addedDevice in addedDevices)
            {
                var itemState = newStateContainer.State.States[addedDevice];

                var trackedStates = GetOnlyTrackedStates(newStateContainer.UntrackedStates, itemState);

                foreach (var telemetryPair in trackedStates)
                {
                    var valueString = GetValueString(telemetryPair.Value);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, itemState.ItemType,
                        itemState.ItemId, telemetryPair.Key, null, valueString));
                }
            }
        }

        private void NotifySubscribersAboutUpdatedDevices(List<string> updatedDevices,
            ApiManagerStateContainer oldStateContainer, ApiManagerStateContainer newStateContainer)
        {
            foreach (var updatedDevice in updatedDevices)
            {
                var newItemState = newStateContainer.State.States[updatedDevice];
                var oldItemState = oldStateContainer.State.States[updatedDevice];

                var newTelemetry = GetOnlyTrackedStates(newStateContainer.UntrackedStates, newItemState);
                var oldTelemetry = GetOnlyTrackedStates(oldStateContainer.UntrackedStates, oldItemState);

                var addedParameters = newTelemetry.Keys.Except(oldTelemetry.Keys).ToList();
                var removedParameters = oldTelemetry.Keys.Except(newTelemetry.Keys).ToList();
                var updatedParameters = newTelemetry.Keys.Except(addedParameters).ToList();

                if (oldItemState.ConnectionStatus != newItemState.ConnectionStatus)
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newItemState.ItemType,
                        newItemState.ItemId, nameof(newItemState.ConnectionStatus),
                        oldItemState.ConnectionStatus.ToString(),
                        newItemState.ConnectionStatus.ToString()));

                foreach (var removedParameter in removedParameters)
                {
                    var oldValueString = GetValueString(oldTelemetry[removedParameter]);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, newItemState.ItemType,
                        newItemState.ItemId, removedParameter, oldValueString, null));
                }

                foreach (var addedParameter in addedParameters)
                {
                    var newValueString = GetValueString(newTelemetry[addedParameter]);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, newItemState.ItemType,
                        newItemState.ItemId, addedParameter, null, newValueString));
                }

                foreach (var updatedParameter in updatedParameters)
                {
                    var areEqual = ObjectsAreEqual(oldTelemetry[updatedParameter], newTelemetry[updatedParameter]);

                    if (!areEqual)
                    {
                        var oldValueString = GetValueString(oldTelemetry[updatedParameter]);
                        var newValueString = GetValueString(newTelemetry[updatedParameter]);

                        if (!_stateContainerTransformer.ParameterIsTransformed(updatedDevice, updatedParameter))
                            NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated,
                                newItemState.ItemType, newItemState.ItemId, updatedParameter, oldValueString,
                                newValueString));
                    }
                }
            }
        }

        private Dictionary<string, object> GetOnlyTrackedStates(IStateGettable item, Dictionary<string, object> states)
        {
            if (item == null || item.UntrackedFields == null || !item.UntrackedFields.Any())
                return states;

            return states.Where(p => !item.UntrackedFields.Contains(p.Key))
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private Dictionary<string, object> GetOnlyTrackedStates(Dictionary<string, IList<string>> untrackedStates,
            IItemState itemState)
        {
            var untrackedFields = untrackedStates.ContainsKey(itemState.ItemId)
                ? untrackedStates[itemState.ItemId]
                : null;

            if (untrackedFields == null || !untrackedFields.Any())
                return itemState.States;

            return itemState.States.Where(p => !untrackedFields.Contains(p.Key))
                            .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private bool ObjectsAreEqual(object obj1, object obj2)
        {
            //TODO test this method. Maybe it's faster just to serialize objects.
            Type type;

            if (obj1 != null)
                type = obj1.GetType();
            else if (obj2 != null)
                type = obj2.GetType();
            else //Both are null => no changes
                return true;

            var obj1IsDict = IsDictionary(obj1);
            var obj2IsDict = IsDictionary(obj2);

            if (obj1IsDict && obj2IsDict)
            {
                var obj1String = JsonConvert.SerializeObject(obj1);
                var obj2String = JsonConvert.SerializeObject(obj2);

                return obj1String == obj2String;
            }
            
            if (obj1IsDict || obj2IsDict)
                return false;

            var comparer = new ObjectsComparer.Comparer();

            var isEqual = comparer.Compare(type, obj1, obj2);

            return isEqual;
        }

        private bool IsDictionary(object obj)
        {
            if (obj == null) 
                return false;

            return obj is IDictionary &&
                   obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        private string GetValueString(object value)
        {
            if (value == null)
                return null;

            if (IsSimpleType(value.GetType()))
                return value.ToString();

            try
            {
                var serialized = JsonConvert.SerializeObject(value);

                return serialized;
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return null;
        }

        public bool IsSimpleType(Type type)
        {
            return
                type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type.IsEnum ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                 IsSimpleType(type.GetGenericArguments()[0]));
        }

        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _stateChangedSubscribers.Add(subscriber);
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            _stateChangedSubscribers.Remove(subscriber);
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
            foreach (var stateChangedSubscriber in _stateChangedSubscribers)
            {
                Task.Run(async () => await stateChangedSubscriber.Notify(args))
                    .ContinueWith(t =>
                    {
                        _logger.Error(t.Exception);
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private class ApiManagerStateContainer
        {
            public IStatesContainer State { get; set; }

            public Dictionary<string, IList<string>> UntrackedStates { get; set; } =
                new Dictionary<string, IList<string>>();
            public Dictionary<string, IList<string>> UncachedStates { get; set; } =
                new Dictionary<string, IList<string>>();

            public ApiManagerStateContainer(IStatesContainer state)
            {
                State = state;
            }
        }
    }
}