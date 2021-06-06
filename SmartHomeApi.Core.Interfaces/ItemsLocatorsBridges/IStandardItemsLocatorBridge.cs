using System;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges
{
    public interface IStandardItemsLocatorBridge : IItemsLocator, IInitializable
    {
        event EventHandler<ItemEventArgs> ItemAdded;
        event EventHandler<ItemEventArgs> ItemDeleted;

        Task ConfigAdded(IItemConfig config);
        Task ConfigUpdated(IItemConfig config);
        Task ConfigDeleted(string itemId);
    }
}