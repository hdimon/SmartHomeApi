using System;

namespace SmartHomeApi.Core.Interfaces
{
    public class StateChangedEvent
    {
        public DateTimeOffset EventDate { get; set; }
        public StateChangedEventType EventType { get; }
        public string ItemType { get; }
        public string ItemId { get; }
        public string Parameter { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public StateChangedEvent()
        {
        }

        public StateChangedEvent(StateChangedEventType eventType, string itemType, string itemId, string parameter,
            object oldValue, object newValue)
        {
            EventType = eventType;
            ItemType = itemType;
            ItemId = itemId;
            Parameter = parameter;
            OldValue = oldValue;
            NewValue = newValue;
            EventDate = DateTimeOffset.Now;
        }

        public override string ToString()
        {
            return "StateChangedEvent [" +
                   $"{nameof(EventDate)}: {EventDate}, " +
                   $"{nameof(EventType)}: {EventType}, " +
                   $"{nameof(ItemType)}: {ItemType}, " +
                   $"{nameof(ItemId)}: {ItemId}, " +
                   $"{nameof(Parameter)}: {Parameter}, " +
                   $"{nameof(OldValue)}: {OldValue}, " +
                   $"{nameof(NewValue)}: {NewValue}" +
                   "]";
        }
    }
}