using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        AppSettings GetConfiguration();
        IItemsPluginsLocator GetItemsPluginsLocator();
        IItemsLocator GetItemsLocator();
        IItemsConfigLocator GetItemsConfigsLocator();
        IApiManager GetApiManager();
        IItemHelpersFabric GetItemHelpersFabric(string itemId);
        IItemHelpersFabric GetItemHelpersFabric(string itemId, string itemType);
        IApiLogger GetApiLogger();
        IDateTimeOffsetProvider GetDateTimeOffsetProvider();
        INotificationsProcessor GetNotificationsProcessor();
        IUncachedStatesProcessor GetUncachedStatesProcessor();
    }
}