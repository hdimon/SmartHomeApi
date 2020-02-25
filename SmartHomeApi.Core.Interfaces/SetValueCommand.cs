namespace SmartHomeApi.Core.Interfaces
{
    public class SetValueCommand : SetValueCommandAbstract
    {
        public string ItemId { get; set; }
        public string Parameter { get; set; }
        public string Value { get; set; }
    }
}