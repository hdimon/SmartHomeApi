using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsConfigLocator : IInitializable
    {
        Task<List<IItemConfig>> GetItemsConfigs(string itemType);
    }
}