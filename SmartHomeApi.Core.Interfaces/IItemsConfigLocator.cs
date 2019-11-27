using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsConfigLocator
    {
        List<IItemConfig> GetItemsConfigs(string itemType);
    }
}