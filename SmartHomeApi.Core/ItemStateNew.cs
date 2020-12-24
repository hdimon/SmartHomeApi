using System.Collections.Generic;
using System.Collections.ObjectModel;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class ItemStateNew : IItemStateNew
    {
        private readonly ItemStateProxy _proxy;
        public string ItemId { get; }
        public string ItemType { get; }

        public ItemStateNew(string itemId, string itemType, ItemStateProxy proxy)
        {
            ItemId = itemId;
            ItemType = itemType;
            _proxy = proxy;
        }

        public void SetState(string key, object value)
        {
            _proxy.SetState(key, value);
        }

        public object GetState(string key)
        {
            return _proxy.GetState(key);
        }

        public void RemoveState(string key)
        {
            _proxy.RemoveState(key);
        }

        public IDictionary<string, object> GetStates()
        {
            var dict = new ReadOnlyDictionary<string, object>(_proxy.GetStates());

            return dict;
        }
    }
}