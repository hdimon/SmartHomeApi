namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        IItemsPluginsLocator GetItemsPluginsLocator();
        IItemsLocator GetItemsLocator();
        IDeviceConfigLocator GetDeviceConfigsLocator();
        IApiManager GetApiManager();
        IDeviceHelpersFabric GetDeviceHelpersFabric();
        IApiLogger GetApiLogger();
    }
}