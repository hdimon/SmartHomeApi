using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemStateNew
    {
        string ItemId { get; }
        string ItemType { get; }

        void SetState(string key, object value);
        object GetState(string key);
        void RemoveState(string key);
        IDictionary<string, object> GetStates();
    }
}