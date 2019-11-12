using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Models;

namespace SmartHomeApi.Core.Services
{
    public class DeviceManager : IDeviceManager
    {
        private readonly ISmartHomeApiFabric _fabric;
        private IDeviceStatesContainer _state;
        private Task _worker;
        private ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private List<IStateChangedSubscriber> _stateChangedSubscribers = new List<IStateChangedSubscriber>();

        public DeviceManager(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _state = CreateDeviceStatesContainer();
        }

        public async Task Initialize()
        {
            await _fabric.GetEventHandlerLocator().Initialize();
            RunStatesCollectorWorker();
        }

        public void Dispose()
        {

        }

        public async Task<ISetValueResult> SetValue(string deviceId, string parameter, string value)
        {
            var deviceLocator = _fabric.GetDeviceLocator();
            var devices = deviceLocator.GetDevices();

            var device = devices.FirstOrDefault(d => d.DeviceId == deviceId);

            if (device == null)
                return new SetValueResult(false);

            NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueSet, device.DeviceType, device.DeviceId,
                parameter, null, value));

            var result = await device.SetValue(parameter, value);

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

        public IDeviceStatesContainer GetState()
        {
            return GetStateSafely();
        }

        public IDeviceState GetState(string deviceId)
        {
            var state = GetStateSafely();

            if (state.DevicesStates.ContainsKey(deviceId))
                return state.DevicesStates[deviceId];

            return new DeviceState(deviceId, string.Empty);
        }

        public object GetState(string deviceId, string parameter)
        {
            var state = GetStateSafely();

            if (state.DevicesStates.ContainsKey(deviceId))
            {
                var deviceState = state.DevicesStates[deviceId];

                if (deviceState.Telemetry.ContainsKey(parameter))
                    return deviceState.Telemetry[parameter];
            }

            return string.Empty;
        }

        private IDeviceStatesContainer CreateDeviceStatesContainer()
        {
            return new DeviceStatesContainer();
        }

        private IDeviceStatesContainer GetStateSafely()
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            IDeviceStatesContainer state = _state;

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

                var deviceLocator = _fabric.GetDeviceLocator();
                var devices = deviceLocator.GetDevices();

                var state = CreateDeviceStatesContainer();

                var deviceTypes = devices.Select(d => d.DeviceType).Distinct().ToList();
                deviceTypes.Sort();

                foreach (var deviceType in deviceTypes)
                {
                    var typeDevices = devices.Where(d => d.DeviceType == deviceType).ToList();
                    typeDevices = typeDevices.OrderBy(d => d.DeviceId).ToList();

                    foreach (var device in typeDevices)
                    {
                        var deviceState = device.GetState();

                        state.DevicesStates.Add(deviceState.DeviceId, deviceState);
                    }
                }

                var oldState = GetState();
                SetStateSafely(state);

                NotifySubscribersAboutChanges(oldState);
            }
        }

        private void SetStateSafely(IDeviceStatesContainer state)
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

        private void NotifySubscribersAboutChanges(IDeviceStatesContainer oldState)
        {
            var newDevices = _state.DevicesStates;
            var oldDevices = oldState.DevicesStates;

            var addedDevices = newDevices.Keys.Except(oldDevices.Keys).ToList();
            var removedDevices = oldDevices.Keys.Except(newDevices.Keys).ToList();
            var updatedDevices = newDevices.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(removedDevices, oldDevices);
            NotifySubscribersAboutAddedDevices(addedDevices, newDevices);
            NotifySubscribersAboutUpdatedDevices(updatedDevices, newDevices, oldDevices);
        }

        private void NotifySubscribersAboutRemovedDevices(List<string> removedDevices,
            Dictionary<string, IDeviceState> oldDevices)
        {
            foreach (var removedDevice in removedDevices)
            {
                var device = oldDevices[removedDevice];

                foreach (var telemetryPair in device.Telemetry)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, device.DeviceType,
                        device.DeviceId, telemetryPair.Key, telemetryPair.Value?.ToString(), null));
                }
            }
        }

        private void NotifySubscribersAboutAddedDevices(List<string> addedDevices,
            Dictionary<string, IDeviceState> newDevices)
        {
            foreach (var addedDevice in addedDevices)
            {
                var device = newDevices[addedDevice];

                foreach (var telemetryPair in device.Telemetry)
                {
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, device.DeviceType,
                        device.DeviceId, telemetryPair.Key, null, telemetryPair.Value?.ToString()));
                }
            }
        }

        private void NotifySubscribersAboutUpdatedDevices(List<string> updatedDevices,
            Dictionary<string, IDeviceState> newDevices, Dictionary<string, IDeviceState> oldDevices)
        {
            foreach (var updatedDevice in updatedDevices)
            {
                var newDevice = newDevices[updatedDevice];
                var oldDevice = oldDevices[updatedDevice];

                var newTelemetry = newDevice.Telemetry;
                var oldTelemetry = oldDevice.Telemetry;

                var addedParameters = newTelemetry.Keys.Except(oldTelemetry.Keys).ToList();
                var removedParameters = oldTelemetry.Keys.Except(newTelemetry.Keys).ToList();
                var updatedParameters = newTelemetry.Keys.Except(addedParameters).ToList();

                if (oldDevice.ConnectionStatus != newDevice.ConnectionStatus)
                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newDevice.DeviceType,
                        newDevice.DeviceId, nameof(newDevice.ConnectionStatus), oldDevice.ConnectionStatus.ToString(),
                        newDevice.ConnectionStatus.ToString()));

                foreach (var removedParameter in removedParameters)
                {
                    var oldValue = oldTelemetry[removedParameter];

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, newDevice.DeviceType,
                        newDevice.DeviceId, removedParameter, oldValue?.ToString(), null));
                }

                foreach (var addedParameter in addedParameters)
                {
                    var newValue = newTelemetry[addedParameter];

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, newDevice.DeviceType,
                        newDevice.DeviceId, addedParameter, null, newValue?.ToString()));
                }

                foreach (var updatedParameter in updatedParameters)
                {
                    var oldValue = oldTelemetry[updatedParameter]?.ToString();
                    var newValue = newTelemetry[updatedParameter]?.ToString();

                    if (oldValue != newValue)
                    {
                        NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, newDevice.DeviceType,
                            newDevice.DeviceId, updatedParameter, oldValue, newValue));
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