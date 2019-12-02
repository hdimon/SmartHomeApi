namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        IItemsPluginsLocator GetItemsPluginsLocator();
        IItemsLocator GetItemsLocator();
        IItemsConfigLocator GetItemsConfigsLocator();
        IApiManager GetApiManager();
        IItemHelpersFabric GetItemHelpersFabric();
        IApiLogger GetApiLogger();
    }
}