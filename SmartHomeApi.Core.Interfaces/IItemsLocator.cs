using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsLocator
    {
        string ItemType { get; }
        bool ImmediateInitialization { get; }
        Task<IEnumerable<IItem>> GetItems();
    }
}