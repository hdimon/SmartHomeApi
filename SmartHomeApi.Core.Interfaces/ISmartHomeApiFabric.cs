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
        IApiLogger GetApiLogger();
        INotificationsProcessor GetNotificationsProcessor();
        IUntrackedStatesProcessor GetUntrackedStatesProcessor();
        IUncachedStatesProcessor GetUncachedStatesProcessor();
    }
}