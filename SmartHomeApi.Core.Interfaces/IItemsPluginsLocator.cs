using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemsPluginsLocator : IDisposable, IInitializable
    {
        event EventHandler<ItemLocatorEventArgs> ItemLocatorAddedOrUpdated;
        event EventHandler<ItemLocatorEventArgs> BeforeItemLocatorDeleted;
        event EventHandler<ItemLocatorEventArgs> ItemLocatorDeleted;

        Task<IEnumerable<IItemsLocator>> GetItemsLocators();
    }
}