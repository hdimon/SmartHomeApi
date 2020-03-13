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
        private IStatesContainer _state;
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
            _state = CreateStatesContainer();
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
            var state = GetStateSafely();

            state = await TransformStateIfRequired(state, transform);

            return state;
        }

        public async Task<IItemState> GetState(string deviceId, bool transform = false)
        {
            var state = GetStateSafely();

            state = await TransformStateIfRequired(state, transform);

            if (state.States.ContainsKey(deviceId))
                return state.States[deviceId];

            return new ItemState(deviceId, string.Empty);
        }

        public async Task<object> GetState(string deviceId, string parameter, bool transform = false)
        {
            var state = GetStateSafely();

            state = await TransformStateIfRequired(state, transform);

            if (state.States.ContainsKey(deviceId))
            {
                var deviceState = state.States[deviceId];

                if (deviceState.States.ContainsKey(parameter))
                    return deviceState.States[parameter];
            }

            return string.Empty;
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

        private IStatesContainer GetStateSafely()
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            IStatesContainer state = _state;

            _readerWriterLock.ReleaseReaderLock();

            return state;
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
                    var itemsLocator = _fabric.GetItemsLocator();
                    var items = await GetItems(itemsLocator);
                    var state = CreateStatesContainer();

                    var gettableItems = items.Where(d => d is IStateGettable).Cast<IStateGettable>()
                                             .OrderBy(i => i.ItemType).ThenBy(i => i.ItemId).ToList();

                    foreach (var item in gettableItems)
                    {
                        try
                        {
                            var deviceState = item.GetState();

                            state.States.Add(deviceState.ItemId, deviceState);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Error when collecting items states.");
                        }
                    }

                    var oldState = await GetState();
                    SetStateSafely(state);

                    NotifySubscribersAboutChanges(oldState, gettableItems);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error when collecting items states.");
                }
            }
        }

        private void SetStateSafely(IStatesContainer state)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

                _state = state;
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

        private void NotifySubscribersAboutChanges(IStatesContainer oldState, List<IStateGettable> gettableItems)
        {
            var newStates = _state.States;
            var oldStates = oldState.States;

            var addedDevices = newStates.Keys.Except(oldStates.Keys).ToList();
            var removedDevices = oldStates.Keys.Except(newStates.Keys).ToList();
            var updatedDevices = newStates.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(gettableItems, removedDevices, oldStates);
            NotifySubscribersAboutAddedDevices(gettableItems, addedDevices, newStates);
            NotifySubscribersAboutUpdatedDevices(gettableItems, updatedDevices, newStates, oldStates);
        }

        private void NotifySubscribersAboutRemovedDevices(List<IStateGettable> gettableItems,
            List<string> removedDevices, Dictionary<string, IItemState> oldStates)
        {
            foreach (var removedDevice in removedDevices)
            {
                var itemState = oldStates[removedDevice];
                var item = gettableItems.FirstOrDefault(i => i.ItemId == itemState.ItemId);

                var trackedStates = GetOnlyTrackedStates(item, itemState.States);

                foreach (var telemetryPair in trackedStates)
                {
                    var valueString = GetValueString(telemetryPair.Value);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, itemState.ItemType,
                        itemState.ItemId, telemetryPair.Key, valueString, null));
                }
            }
        }

        private void NotifySubscribersAboutAddedDevices(List<IStateGettable> gettableItems, List<string> addedDevices,
            Dictionary<string, IItemState> newStates)
        {
            foreach (var addedDevice in addedDevices)
            {
                var itemState = newStates[addedDevice];
                var item = gettableItems.FirstOrDefault(i => i.ItemId == itemState.ItemId);

                var trackedStates = GetOnlyTrackedStates(item, itemState.States);

                foreach (var telemetryPair in trackedStates)
                {
                    var valueString = GetValueString(telemetryPair.Value);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, itemState.ItemType,
                        itemState.ItemId, telemetryPair.Key, null, valueString));
                }
            }
        }

        private void NotifySubscribersAboutUpdatedDevices(List<IStateGettable> gettableItems,
            List<string> updatedDevices, Dictionary<string, IItemState> newStates,
            Dictionary<string, IItemState> oldStates)
        {
            foreach (var updatedDevice in updatedDevices)
            {
                var newItemState = newStates[updatedDevice];
                var oldItemState = oldStates[updatedDevice];

                var item = gettableItems.FirstOrDefault(i => i.ItemId == oldItemState.ItemId);

                var newTelemetry = GetOnlyTrackedStates(item, newItemState.States);
                var oldTelemetry = GetOnlyTrackedStates(item, oldItemState.States);

                var addedParameters = newTelemetry.Keys.Except(oldTelemetry.Keys).ToList();
                var removedParameters = oldTelemetry.Keys.Except(newTelemetry.Keys).ToList();
                var updatedParameters = newTelemetry.Keys.Except(addedParameters).ToList();

                if (oldItemState.ConnectionStatus != newItemState.ConnectionStatus)
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newItemState.ItemType,
                        newItemState.ItemId, nameof(newItemState.ConnectionStatus), oldItemState.ConnectionStatus.ToString(),
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
    }
}