namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        IItemsPluginsLocator GetItemsPluginsLocator();
        IItemsLocator GetItemsLocator();
        IItemsConfigLocator GetDeviceConfigsLocator();
        IApiManager GetApiManager();
        IDeviceHelpersFabric GetDeviceHelpersFabric();
        IApiLogger GetApiLogger();
    }
}