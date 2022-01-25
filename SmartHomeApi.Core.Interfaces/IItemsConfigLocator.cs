using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsConfigLocator : IInitializable, IAsyncDisposable
    {
        Task<List<IItemConfig>> GetItemsConfigs(string itemType);
    }
}