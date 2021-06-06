using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiItemsLocator : IInitializable
    {
        Task<IEnumerable<IItem>> GetItems();
    }
}