using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        AppSettings GetConfiguration();
        IItemsPluginsLocator GetItemsPluginsLocator();
        IApiItemsLocator GetApiItemsLocator();
        IItemsConfigLocator GetItemsConfigsLocator();
        IApiManager GetApiManager();
        IItemHelpersFabric GetItemHelpersFabric(string itemId, string itemType);
        IApiLogger GetApiLogger();
        IDateTimeOffsetProvider GetDateTimeOffsetProvider();
        INotificationsProcessor GetNotificationsProcessor();
        IUncachedStatesProcessor GetUncachedStatesProcessor();
        IDynamicToObjectMapper GetDynamicToObjectMapper();
        IObjectToDynamicConverter GetObjectToDynamicConverter();
    }
}