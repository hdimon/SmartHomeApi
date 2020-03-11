using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsConfigLocator : IInitializable, IDisposable
    {
        Task<List<IItemConfig>> GetItemsConfigs(string itemType);
    }
}