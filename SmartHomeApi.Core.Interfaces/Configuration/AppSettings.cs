using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHomeApi.Core.Interfaces.Configuration
{
    public class AppSettings : ICloneable
    {
        public bool SoftPluginsLoading { get; set; }
        public string DataDirectoryPath { get; set; }
        public List<AppSettingItemInfo> UntrackedItems { get; set; } = new List<AppSettingItemInfo>();
        public List<AppSettingItemInfo> UncachedItems { get; set; } = new List<AppSettingItemInfo>();
        public object Clone()
        {
            var clone = new AppSettings();

            clone.SoftPluginsLoading = SoftPluginsLoading;
            clone.DataDirectoryPath = DataDirectoryPath;
            clone.UntrackedItems = UntrackedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();
            clone.UncachedItems = UncachedItems.Select(i => (AppSettingItemInfo)i.Clone()).ToList();

            return clone;
        }
    }
}