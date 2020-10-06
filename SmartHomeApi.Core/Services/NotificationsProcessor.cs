using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.Services
{
    public class NotificationsProcessor : INotificationsProcessor
    {
        private readonly List<IStateChangedSubscriber> _stateChangedSubscribers = new List<IStateChangedSubscriber>();
        private readonly IStatesContainerTransformer _stateContainerTransformer;
        private readonly IApiLogger _logger;

        public string ItemType => null;
        public string ItemId => null;

        public NotificationsProcessor(ISmartHomeApiFabric fabric)
        {
            _logger = fabric.GetApiLogger();
            _stateContainerTransformer = fabric.GetStateContainerTransformer();
        }

        public void NotifySubscribersAboutChanges(ApiManagerStateContainer oldStateContainer,
            ApiManagerStateContainer newStateContainer)
        {
            var newStates = newStateContainer.State.States;
            var oldStates = oldStateContainer.State.States;

            var addedDevices = newStates.Keys.Except(oldStates.Keys).ToList();
            var removedDevices = oldStates.Keys.Except(newStates.Keys).ToList();
            var updatedDevices = newStates.Keys.Except(addedDevices).ToList();

            NotifySubscribersAboutRemovedDevices(removedDevices, oldStateContainer);
            NotifySubscribersAboutAddedDevices(addedDevices, newStateContainer);
            NotifySubscribersAboutUpdatedDevices(updatedDevices, oldStateContainer, newStateContainer);
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
                        itemState.ItemId, telemetryPair.Key, valueString, null, telemetryPair.Value, null));
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
                        itemState.ItemId, telemetryPair.Key, null, valueString, null, telemetryPair.Value));
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
                        newItemState.ConnectionStatus.ToString(),
                        oldItemState.ConnectionStatus,
                        newItemState.ConnectionStatus));

                foreach (var removedParameter in removedParameters)
                {
                    var oldValueString = GetValueString(oldTelemetry[removedParameter]);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, newItemState.ItemType,
                        newItemState.ItemId, removedParameter, oldValueString, null, oldTelemetry[removedParameter],
                        null));
                }

                foreach (var addedParameter in addedParameters)
                {
                    var newValueString = GetValueString(newTelemetry[addedParameter]);

                    NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, newItemState.ItemType,
                        newItemState.ItemId, addedParameter, null, newValueString, null, newTelemetry[addedParameter]));
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
                                newValueString, oldTelemetry[updatedParameter], newTelemetry[updatedParameter]));
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
                    .ContinueWith(t => { _logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private Dictionary<string, object> GetOnlyTrackedStates(Dictionary<string, AppSettingItemInfo> untrackedStates,
            IItemState itemState)
        {
            if (!untrackedStates.ContainsKey(itemState.ItemId))
                return itemState.States;

            var untrackedFields = untrackedStates[itemState.ItemId];

            if (!untrackedFields.ApplyOnlyEnumeratedStates) //It means item is not tracked at all
                return new Dictionary<string, object>();

            if (!untrackedFields.States.Any())
                return itemState.States;

            return itemState.States.Where(p => !untrackedFields.States.Contains(p.Key))
                            .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private string GetValueString(object value)
        {
            if (value == null)
                return null;

            if (TypeHelper.IsSimpleType(value.GetType()))
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

            var obj1IsDict = TypeHelper.IsDictionary(obj1);
            var obj2IsDict = TypeHelper.IsDictionary(obj2);

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
    }
}