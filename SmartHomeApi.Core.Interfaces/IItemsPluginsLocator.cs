using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsPluginsLocator : IDisposable, IInitializable
    {
        Task<IEnumerable<IItemsLocator>> GetItemsLocators();
    }
}