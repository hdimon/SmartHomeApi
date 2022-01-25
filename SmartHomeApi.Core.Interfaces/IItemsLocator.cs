using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsLocator : IAsyncDisposable
    {
        string ItemType { get; }
        Type ConfigType { get; }
        Task<IEnumerable<IItem>> GetItems();
    }
}