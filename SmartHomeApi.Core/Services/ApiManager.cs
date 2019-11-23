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

        public string ItemType => null;
        public string ItemId => null;

        public ApiManager(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _state = CreateStatesContainer();
        }

        public async Task Initialize()
        {
            var locators = await _fabric.GetItemsPluginsLocator().GetItemsLocators();
            var immediateItems = locators.Where(l => l.ImmediateInitialization).ToList();

            foreach (var immediateItem in immediateItems)
            {
                await immediateItem.GetItems();
            }

            RunStatesCollectorWorker();
        }

        public void Dispose()
        {

        }

        public async Task<ISetValueResult> SetValue(string deviceId, string parameter, string value)
        {
            var itemsLocator = _fabric.GetItemsLocator();
            var items = await itemsLocator.GetItems();

            var item = items.FirstOrDefault(i => i is IStateSettable it && it.ItemId == deviceId);

            if (item == null)
                return new SetValueResult(false);

            var not = (IStateSettable)item;

            /*var deviceLocator = _fabric.GetDeviceLocator();
            var devices = deviceLocator.GetDevices();

            var device = devices.FirstOrDefault(d => d.DeviceId == deviceId);

            if (device == null)
                return new SetValueResult(false);*/

            NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueSet, not.ItemType, not.ItemId,
                parameter, null, value));

            var result = await not.SetValue(parameter, value);

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

        public IStatesContainer GetState()
        {
            return GetStateSafely();
        }

        public IItemState GetState(string deviceId)
        {
            var state = GetStateSafely();

            if (state.States.ContainsKey(deviceId))
                return state.States[deviceId];

            return new ItemState(deviceId, string.Empty);
        }

        public object GetState(string deviceId, string parameter)
        {
            var state = GetStateSafely();

            if (state.States.ContainsKey(deviceId))
            {
                var deviceState = state.States[deviceId];

                if (deviceState.States.ContainsKey(parameter))
                    return deviceState.States[parameter];
            }

            return string.Empty;
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
                _worker = Task.Factory.StartNew(CollectDevicesStates).Unwrap().ContinueWith(
                    t => { } /*Log.Error(t.Exception)*/,
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task CollectDevicesStates()
        {
            while (true)
            {
                await Task.Delay(500);

                var deviceLocator = _fabric.GetItemsLocator();
                var devices = await deviceLocator.GetItems();
                var state = CreateStatesContainer();

                var items = devices.Where(d => d is IStateGettable).Cast<IStateGettable>().ToList();

                var deviceTypes = items.Select(d => d.ItemType).Distinct().ToList();
                deviceTypes.Sort();

                foreach (var deviceType in deviceTypes)
                {
                    var typeDevices = items.Where(d => d.ItemType == deviceType).ToList();
                    typeDevices = typeDevices.OrderBy(d => d.ItemId).ToList();

                    foreach (var device in typeDevices)
                    {
                        var deviceState = device.GetState();

                        state.States.Add(deviceState.ItemId, deviceState);
                    }
                }

                var oldState = GetState();
                SetStateSafely(state);

                NotifySubscribersAboutChanges(oldState);
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
                /*Console.WriteLine(e);
                throw;*/
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        private void NotifySubscribersAboutChanges(IStatesContainer oldState)
        {
            var newDevices = _state.States;
            var oldDevices = oldState.States;

            var addedDevices = newDevices.Keys.Except(oldDevices.Keys).ToList();
            var removedDevices = oldDevices.Keys.Except(newDevices.Keys).ToList();
            var updatedDevices = newDevices.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(removedDevices, oldDevices);
            NotifySubscribersAboutAddedDevices(addedDevices, newDevices);
            NotifySubscribersAboutUpdatedDevices(updatedDevices, newDevices, oldDevices);
        }

        private void NotifySubscribersAboutRemovedDevices(List<string> removedDevices,
            Dictionary<string, IItemState> oldDevices)
        {
            foreach (var removedDevice in removedDevices)
            {
                var device = oldDevices[removedDevice];

                foreach (var telemetryPair in device.States)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, device.ItemType,
                        device.ItemId, telemetryPair.Key, telemetryPair.Value?.ToString(), null));
                }
            }
        }

        private void NotifySubscribersAboutAddedDevices(List<string> addedDevices,
            Dictionary<string, IItemState> newDevices)
        {
            foreach (var addedDevice in addedDevices)
            {
                var device = newDevices[addedDevice];

                foreach (var telemetryPair in device.States)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, device.ItemType,
                        device.ItemId, telemetryPair.Key, null, telemetryPair.Value?.ToString()));
                }
            }
        }

        private void NotifySubscribersAboutUpdatedDevices(List<string> updatedDevices,
            Dictionary<string, IItemState> newDevices, Dictionary<string, IItemState> oldDevices)
        {
            foreach (var updatedDevice in updatedDevices)
            {
                var newDevice = newDevices[updatedDevice];
                var oldDevice = oldDevices[updatedDevice];

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
                        NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newDevice.ItemType,
                            newDevice.ItemId, updatedParameter, oldValue, newValue));
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
                Task.Run(async () => await stateChangedSubscriber.Notify(args)).ContinueWith(t =>
                {
                    /*ILog log = ServiceLocator.Current.GetInstance<ILog>();
                    log.Error("Unexpected Error", t.Exception);*/
                    var test = 5;
                }, TaskContinuationOptions.OnlyOnFaulted);
                //stateChangedSubscriber.Notify(args);
            }
        }
    }
}