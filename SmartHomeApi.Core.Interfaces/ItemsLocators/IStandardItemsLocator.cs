using System;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces.ItemsLocators
{
    public interface IStandardItemsLocator : IItemsLocator, IInitializable
    {
        event EventHandler<ItemEventArgs> ItemAdded;
        event EventHandler<ItemEventArgs> ItemDeleted;

        Task ConfigAdded(IItemConfig config);
        Task ConfigUpdated(IItemConfig config);
        Task ConfigDeleted(string itemId);
    }
}