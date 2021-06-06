using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IConfigurable : IItem
    {
        IItemConfig Config { get; }
        void OnConfigChange(IItemConfig newConfig, IEnumerable<ItemConfigChangedField> changedFields = null);
    }
}