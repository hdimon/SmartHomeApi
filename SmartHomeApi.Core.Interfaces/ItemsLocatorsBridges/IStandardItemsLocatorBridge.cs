using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges
{
    public interface IStandardItemsLocatorBridge : IItemsLocator
    {
        Task ConfigAdded(IItemConfig config);
        Task ConfigUpdated(IItemConfig config);
        Task ConfigDeleted(string itemId);
    }
}