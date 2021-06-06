using System;

namespace SmartHomeApi.Core.Interfaces
{
    public class ItemEventArgs : EventArgs
    {
        public string ItemId { get; }
        public string ItemType { get; }

        public ItemEventArgs(string itemId, string itemType)
        {
            ItemId = itemId;
            ItemType = itemType;
        }
    }
}