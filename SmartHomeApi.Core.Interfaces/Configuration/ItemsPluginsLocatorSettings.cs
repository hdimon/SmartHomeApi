using System;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class ItemsPluginsLocatorSettings : ICloneable
    {
        public bool SoftPluginsLoading { get; set; }
        public int UnloadPluginsMaxTries { get; set; }
        public int UnloadPluginsTriesIntervalMS { get; set; }
        public int PluginsLoadingTimeMs { get; set; }
        public int PluginsUnloadingAttemptsIntervalMs { get; set; }
        public int ItemLocatorConstructorTimeoutMS { get; set; }

        public object Clone()
        {
            var clone = new ItemsPluginsLocatorSettings();

            clone.SoftPluginsLoading = SoftPluginsLoading;
            clone.UnloadPluginsMaxTries = UnloadPluginsMaxTries;
            clone.UnloadPluginsTriesIntervalMS = UnloadPluginsTriesIntervalMS;
            clone.PluginsLoadingTimeMs = PluginsLoadingTimeMs;
            clone.PluginsUnloadingAttemptsIntervalMs = PluginsUnloadingAttemptsIntervalMs;
            clone.ItemLocatorConstructorTimeoutMS = ItemLocatorConstructorTimeoutMS;

            return clone;
        }
    }
}