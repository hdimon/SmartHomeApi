using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public string ItemType => null;
        public string ItemId => null;

        public ApiManager(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();
            _state = CreateStatesContainer();
            _stateContainerTransformer = _fabric.GetStateContainerTransformer();
        }

        public bool IsInitialized { get; set; }

        public async Task Initialize()
        {
            _logger.Info("Starting ApiManager initialization...");

            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();
            var immediateItems = locators.Where(l => l.ImmediateInitialization).ToList();

            foreach (var immediateItem in immediateItems)
            {
                await GetItems(immediateItem);
            }

            RunStatesCollectorWorker();

            IsInitialized = true;

            _logger.Info("ApiManager initialized.");
        }

        private async Task<IEnumerable<IItem>> GetItems(IItemsLocator locator)
        {
            var items = await locator.GetItems();
            var itemsList = items.ToList();

            foreach (var item in itemsList)
            {
                if (item is IInitializable initializable && !initializable.IsInitialized)
                    await initializable.Initialize();
            }

            return itemsList;
        }

        public void Dispose()
        {

        }

        public async Task<ISetValueResult> SetValue(string deviceId, string parameter, string value)
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await GetItems(itemsLocator);

            var item = items.FirstOrDefault(i => i is IStateSettable it && it.ItemId == deviceId);

            if (item == null)
                return new SetValueResult(false);

            var not = (IStateSettable)item;

            var ev = new StateChangedEvent(StateChangedEventType.ValueSet, not.ItemType, not.ItemId, parameter, null,
                value);

            _stateContainerTransformer.AddStateChangedEvent(ev);

            NotifySubscribers(ev);

            var result = await not.SetValue(parameter, value);

            _stateContainerTransformer.RemoveStateChangedEvent(ev);

            return result;
        }

        public async Task<ISetValueResult> Increase(string deviceId, string parameter)
        {
            return new SetValueResult();
        }

        public async Task<ISetValueResult> Decrease(string deviceId, string parameter)
        {
            return new SetValueResult();
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
            while (true)
            {
                await Task.Delay(250);

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

                    NotifySubscribersAboutChanges(oldState);
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

        private void NotifySubscribersAboutChanges(IStatesContainer oldState)
        {
            var newStates = _state.States;
            var oldStates = oldState.States;

            var addedDevices = newStates.Keys.Except(oldStates.Keys).ToList();
            var removedDevices = oldStates.Keys.Except(newStates.Keys).ToList();
            var updatedDevices = newStates.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(removedDevices, oldStates);
            NotifySubscribersAboutAddedDevices(addedDevices, newStates);
            NotifySubscribersAboutUpdatedDevices(updatedDevices, newStates, oldStates);
        }

        private void NotifySubscribersAboutRemovedDevices(List<string> removedDevices,
            Dictionary<string, IItemState> oldStates)
        {
            foreach (var removedDevice in removedDevices)
            {
                var device = oldStates[removedDevice];

                foreach (var telemetryPair in device.States)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, device.ItemType,
                        device.ItemId, telemetryPair.Key, telemetryPair.Value?.ToString(), null));
                }
            }
        }

        private void NotifySubscribersAboutAddedDevices(List<string> addedDevices,
            Dictionary<string, IItemState> newStates)
        {
            foreach (var addedDevice in addedDevices)
            {
                var device = newStates[addedDevice];

                foreach (var telemetryPair in device.States)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, device.ItemType,
                        device.ItemId, telemetryPair.Key, null, telemetryPair.Value?.ToString()));
                }
            }
        }

        private void NotifySubscribersAboutUpdatedDevices(List<string> updatedDevices,
            Dictionary<string, IItemState> newStates, Dictionary<string, IItemState> oldStates)
        {
            foreach (var updatedDevice in updatedDevices)
            {
                var newDevice = newStates[updatedDevice];
                var oldDevice = oldStates[updatedDevice];

                var newTelemetry = newDevice.States;
                var oldTelemetry = oldDevice.States;

                var addedParameters = newTelemetry.Keys.Except(oldTelemetry.Keys).ToList();
                var removedParameters = oldTelemetry.Keys.Except(newTelemetry.Keys).ToList();
                var updatedParameters = newTelemetry.Keys.Except(addedParameters).ToList();

                if (oldDevice.ConnectionStatus != newDevice.ConnectionStatus)
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newDevice.ItemType,
                        newDevice.ItemId, nameof(newDevice.ConnectionStatus), oldDevice.ConnectionStatus.ToString(),
                        newDevice.ConnectionStatus.ToString()));

                foreach (var removedParameter in removedParameters)
                {
                    var oldValue = oldTelemetry[removedParameter];

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, newDevice.ItemType,
                        newDevice.ItemId, removedParameter, oldValue?.ToString(), null));
                }

                foreach (var addedParameter in addedParameters)
                {
                    var newValue = newTelemetry[addedParameter];

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, newDevice.ItemType,
                        newDevice.ItemId, addedParameter, null, newValue?.ToString()));
                }

                foreach (var updatedParameter in updatedParameters)
                {
                    var oldValue = oldTelemetry[updatedParameter]?.ToString();
                    var newValue = newTelemetry[updatedParameter]?.ToString();

                    if (oldValue != newValue)
                    {
                        if (!_stateContainerTransformer.ParameterIsTransformed(updatedDevice, updatedParameter))
                            NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated,
                                newDevice.ItemType, newDevice.ItemId, updatedParameter, oldValue, newValue));
                    }
                }
            }
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