﻿using System;

namespace SmartHomeApi.Core.Interfaces
{
    public class StateChangedEvent
    {
        public DateTimeOffset EventDate { get; set; }
        public StateChangedEventType EventType { get; }
        public string ItemType { get; }
        public string ItemId { get; }
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
            ItemType = deviceType;
            ItemId = deviceId;
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
                   $"{nameof(ItemId)}: {ItemId}, " +
                   $"{nameof(Parameter)}: {Parameter}, " +
                   $"{nameof(OldValue)}: {OldValue}, " +
                   $"{nameof(NewValue)}: {NewValue}" +
                   "]";
        }
    }
}