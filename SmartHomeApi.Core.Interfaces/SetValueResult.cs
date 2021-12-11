using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public class SetValueResult : ISetValueResult
    {
        public string ItemId { get; }
        public string ItemType { get; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }

        public SetValueResult(bool success = true)
        {
            Success = success;

            if (!success) Errors = new List<string>();
        }

        public SetValueResult(string itemId, string itemType, bool success = true) : this(success)
        {
            ItemId = itemId;
            ItemType = itemType;
        }
    }
}