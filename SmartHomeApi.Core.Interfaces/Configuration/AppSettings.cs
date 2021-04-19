using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class AppSettings : ICloneable
    {
        public string ApiCulture { get; set; }
        public string DataDirectoryPath { get; set; }
        public ItemsPluginsLocatorSettings ItemsPluginsLocator { get; set; } = new ItemsPluginsLocatorSettings();

        public int? ConfigsLocatorIntervalMs { get; set; }
        public int? ItemsLocatorIntervalMs { get; set; }
        public List<AppSettingItemInfo> UntrackedItems { get; set; } = new List<AppSettingItemInfo>();
        public List<AppSettingItemInfo> UncachedItems { get; set; } = new List<AppSettingItemInfo>();

        public object Clone()
        {
            var clone = new AppSettings();

            clone.ApiCulture = ApiCulture;
            clone.ItemsPluginsLocator = (ItemsPluginsLocatorSettings)ItemsPluginsLocator.Clone();
            clone.DataDirectoryPath = DataDirectoryPath;
            clone.ConfigsLocatorIntervalMs = ConfigsLocatorIntervalMs;
            clone.ItemsLocatorIntervalMs = ItemsLocatorIntervalMs;
            clone.UntrackedItems = UntrackedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();
            clone.UncachedItems = UncachedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();

            return clone;
        }
    }
}