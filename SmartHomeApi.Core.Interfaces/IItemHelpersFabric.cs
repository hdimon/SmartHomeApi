namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemHelpersFabric
    {
        IItemStateStorageHelper GetDeviceStateStorageHelper();
        IApiLogger GetApiLogger();
    }
}