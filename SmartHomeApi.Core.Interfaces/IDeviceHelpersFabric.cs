namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceHelpersFabric
    {
        IDeviceStateStorageHelper GetDeviceStateStorageHelper();
        IApiLogger GetApiLogger();
    }
}