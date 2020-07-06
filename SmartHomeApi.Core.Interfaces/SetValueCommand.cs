namespace SmartHomeApi.Core.Interfaces
{
    public class SetValueCommand : SetValueCommandAbstract
    {
        public string ItemId { get; set; }
        public string Parameter { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return "SetValueCommand [" +
                   $"{nameof(ItemId)}: {ItemId}, " +
                   $"{nameof(Parameter)}: {Parameter}, " +
                   $"{nameof(Value)}: {Value}" +
                   "]";
        }
    }
}