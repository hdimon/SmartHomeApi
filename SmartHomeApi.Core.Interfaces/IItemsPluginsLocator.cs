using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsPluginsLocator
    {
        Task<IEnumerable<IItemsLocator>> GetItemsLocators();
    }
}