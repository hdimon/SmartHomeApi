using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsPluginsLocator : IDisposable
    {
        Task<IEnumerable<IItemsLocator>> GetItemsLocators();
    }
}