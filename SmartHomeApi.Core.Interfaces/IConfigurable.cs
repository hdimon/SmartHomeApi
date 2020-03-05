using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IConfigurable
    {
        string ItemType { get; }
        IItemConfig Config { get; }
        void OnConfigChange(IItemConfig newConfig, IEnumerable<ItemConfigChangedField> changedFields = null);
    }
}