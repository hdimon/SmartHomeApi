using System;

namespace SmartHomeApi.Core.Interfaces
{
    public class StateChangedEvent
    {
        public DateTimeOffset EventDate { get; set; }
        public StateChangedEventType EventType { get; }
        public string DeviceType { get; }
        public string DeviceId { get; }
        public string Parameter { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public StateChangedEvent()
        {
        }

        public StateChangedEvent(StateChangedEventType eventType, string deviceType, string deviceId, string parameter,
            string oldValue, string newValue)
        {
            EventType = eventType;
            DeviceType = deviceType;
            DeviceId = deviceId;
            Parameter = parameter;
            OldValue = oldValue;
            NewValue = newValue;
            EventDate = DateTimeOffset.Now;
        }
    }
}