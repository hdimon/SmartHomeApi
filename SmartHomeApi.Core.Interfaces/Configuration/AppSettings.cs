using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class AppSettings : ICloneable
    {
        public string Version { get; set; }
        public string ApiCulture { get; set; }
        public string DataDirectoryPath { get; set; }
        public ItemsPluginsLocatorSettings ItemsPluginsLocator { get; set; } = new ItemsPluginsLocatorSettings();
        public ItemsConfigsLocatorSettings ItemsConfigsLocator { get; set; } = new ItemsConfigsLocatorSettings();

        public List<AppSettingItemInfo> UntrackedItems { get; set; } = new List<AppSettingItemInfo>();
        public List<AppSettingItemInfo> UncachedItems { get; set; } = new List<AppSettingItemInfo>();
        public List<AppSettingInitPriorityItem> ItemsInitPriority { get; set; } = new List<AppSettingInitPriorityItem>();

        public object Clone()
        {
            var clone = new AppSettings();

            clone.Version = Version;
            clone.ApiCulture = ApiCulture;
            clone.ItemsPluginsLocator = (ItemsPluginsLocatorSettings)ItemsPluginsLocator.Clone();
            clone.ItemsConfigsLocator = (ItemsConfigsLocatorSettings)ItemsConfigsLocator.Clone();
            clone.DataDirectoryPath = DataDirectoryPath;
            clone.UntrackedItems = UntrackedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();
            clone.UncachedItems = UncachedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();
            clone.ItemsInitPriority = ItemsInitPriority.Select(i => (AppSettingInitPriorityItem)i.Clone()).ToList();

            return clone;
        }
    }
}