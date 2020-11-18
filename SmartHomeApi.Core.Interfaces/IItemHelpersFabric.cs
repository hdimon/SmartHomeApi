namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemHelpersFabric
    {
        IItemStateStorageHelper GetItemStateStorageHelper();
        IApiLogger GetApiLogger();
    }
}