using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsConfigLocator : IInitializable
    {
        List<IItemConfig> GetItemsConfigs(string itemType);
    }
}