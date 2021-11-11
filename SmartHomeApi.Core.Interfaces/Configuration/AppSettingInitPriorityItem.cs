using System;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class AppSettingInitPriorityItem : ICloneable
    {
        public string ItemId { get; set; }

        public object Clone()
        {
            var clone = new AppSettingInitPriorityItem();

            clone.ItemId = ItemId;

            return clone;
        }
    }
}