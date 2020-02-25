namespace SmartHomeApi.Core.Interfaces
{
    public class SetValueResult : ISetValueResult
    {
        public string ItemId { get; }
        public string ItemType { get; }
        public bool Success { get; set; }

        public SetValueResult(bool success = true)
        {
            Success = success;
        }

        public SetValueResult(string itemId, string itemType, bool success = true)
        {
            ItemId = itemId;
            ItemType = itemType;
            Success = success;
        }
    }
}