using System.Collections.Concurrent;
using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class ItemStateProxy
    {
        private readonly string _itemId;
        private readonly string _itemType;
        private readonly ISmartHomeApiFabric _fabric;
        private readonly INotificationsProcessor _notificationsProcessor;
        private readonly ConcurrentDictionary<string, object> _states;

        public ItemStateProxy(string itemId, string itemType, ISmartHomeApiFabric fabric, ConcurrentDictionary<string, object> states)
        {
            _itemId = itemId;
            _itemType = itemType;
            _fabric = fabric;
            _notificationsProcessor = _fabric.GetNotificationsProcessor();

            _states = states;
        }

        public void SetState(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            object oldValue = null;
            bool isAdded = true;

            var setValue = _states.AddOrUpdate(key, value, (param, old) =>
            {
                oldValue = old;
                isAdded = false;

                return value;
            });

            _notificationsProcessor.NotifySubscribers(new StateChangedEvent(
                isAdded ? StateChangedEventType.ValueAdded : StateChangedEventType.ValueUpdated, _itemType, _itemId, key,
                oldValue, setValue));
        }

        public object GetState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (_states.TryGetValue(key, out var value))
                return value;

            return null;
        }

        public void RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_states.TryRemove(key, out var oldValue))
                return;

            _notificationsProcessor.NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueRemoved, _itemType,
                _itemId, key, oldValue, null));
        }

        public IDictionary<string, object> GetStates()
        {
            return _states;
        }
    }
}