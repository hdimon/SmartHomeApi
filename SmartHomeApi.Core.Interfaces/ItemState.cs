using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public class ItemState : IItemState
    {
        public string ItemId { get; }
        public string ItemType { get; }

        public Dictionary<string, object> States { get; set; } = new Dictionary<string, object>();

        public ItemState(string itemId, string itemType)
        {
            ItemId = itemId;
            ItemType = itemType;
        }

        public object Clone()
        {
            var clone = new ItemState(ItemId, ItemType);

            foreach (var state in States)
            {
                clone.States.Add(state.Key, state.Value);
            }

            return clone;
        }
    }
}