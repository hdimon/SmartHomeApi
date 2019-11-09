namespace SmartHomeApi.Core.Interfaces
{
    public interface ISmartHomeApiFabric
    {
        IDevicePluginLocator GetDevicePluginLocator();
        IDeviceLocator GetDeviceLocator();
        IDeviceConfigLocator GetDeviceConfigsLocator();
        IRequestProcessor GetRequestProcessor();
        IDeviceManager GetDeviceManager();
        IEventHandlerLocator GetEventHandlerLocator();
        IDeviceHelpersFabric GetDeviceHelpersFabric();
    }
}