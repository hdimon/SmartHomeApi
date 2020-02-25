namespace SmartHomeApi.Core.Interfaces
{
    public interface ISetValueResult
    {
        public string ItemId { get; }
        public string ItemType { get; }
        bool Success { get; set; }
    }
}