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
            object oldValue = null;

            var setValue = _states.AddOrUpdate(key, value, (param, old) =>
            {
                oldValue = old;

                return value;
            });
            //_states.AddOrUpdate(key, (s) => AddValueFactory(s, value), (s, oldValue) => UpdateValueFactory(s, oldValue, value));

            _notificationsProcessor.NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, _itemType, _itemId,
                key, oldValue, setValue));
        }

        /*private object AddValueFactory(string key, object value)
        {
            _notificationsProcessor.NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueAdded, _itemType, _itemId,
                key, null, value));

            return value;
        }

        private object UpdateValueFactory(string key, object oldValue, object value)
        {
            _notificationsProcessor.NotifySubscribers(new StateChangedEvent(StateChangedEventType.ValueUpdated, _itemType,
                _itemId, key, oldValue, value));

            return value;
        }*/

        public object GetState(string key)
        {
            if (_states.TryGetValue(key, out var value))
                return value;

            return null;
        }

        public void RemoveState(string key)
        {
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