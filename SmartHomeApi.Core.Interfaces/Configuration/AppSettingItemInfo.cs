using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class AppSettingItemInfo : ICloneable
    {
        public string ItemId { get; set; }
        public bool ApplyOnlyEnumeratedStates { get; set; }
        public List<string> States { get; set; } = new List<string>();

        public object Clone()
        {
            var clone = new AppSettingItemInfo();

            clone.ItemId = ItemId;
            clone.ApplyOnlyEnumeratedStates = ApplyOnlyEnumeratedStates;
            clone.States = States.ToList();

            return clone;
        }
    }
}