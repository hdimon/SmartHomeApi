using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces.ItemsLocators
{
    public interface IStandardItemsLocator : IItemsLocator
    {
        Task ConfigAdded(IItemConfig config);
        Task ConfigUpdated(IItemConfig config);
        Task ConfigDeleted(string itemId);
    }
}