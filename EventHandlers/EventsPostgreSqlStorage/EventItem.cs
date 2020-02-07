using System;
using SmartHomeApi.Core.Interfaces;

namespace EventsPostgreSqlStorage
{
    class EventItem
    {
        public DateTimeOffset EventDate { get; set; }
        public string EventType { get; set; }
        public string DeviceType { get; set; }
        public string DeviceId { get; set; }
        public string Parameter { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public EventItem()
        {
            EventDate = DateTimeOffset.Now;
        }

        public EventItem(StateChangedEvent evt)
        {
            EventType = evt.EventType.ToString();
            DeviceType = evt.ItemType;
            DeviceId = evt.ItemId;
            Parameter = evt.Parameter;
            OldValue = evt.OldValue;
            NewValue = evt.NewValue;
            EventDate = DateTimeOffset.Now;
        }
    }
}