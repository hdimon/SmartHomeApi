using System;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class ItemsConfigsLocatorSettings : ICloneable
    {
        public int ConfigsLoadingDelayMs { get; set; }

        public object Clone()
        {
            var clone = new ItemsConfigsLocatorSettings();

            clone.ConfigsLoadingDelayMs = ConfigsLoadingDelayMs;

            return clone;
        }
    }
}