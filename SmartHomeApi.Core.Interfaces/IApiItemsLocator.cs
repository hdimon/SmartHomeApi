using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IApiItemsLocator : IInitializable, IAsyncDisposable
    {
        /// <summary>
        /// This event is synchronous so it's better to create asynchronous event handler like ItemAdded += async (sender, args) => {}
        /// in order not to block event emitting thread
        /// </summary>
        event EventHandler<ItemEventArgs> ItemAdded;
        /// <summary>
        /// This event is synchronous so it's better to create asynchronous event handler like ItemDeleted += async (sender, args) => {}
        /// in order not to block event emitting thread
        /// </summary>
        event EventHandler<ItemEventArgs> ItemDeleted;

        Task<IEnumerable<IItem>> GetItems();
    }
}