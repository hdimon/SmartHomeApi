namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemHelpersFabric
    {
        IItemStateStorageHelper GetItemStateStorageHelper();
        IJsonSerializer GetJsonSerializer();
        IApiLogger GetApiLogger();
        IItemStateNew GetOrCreateItemState();
    }
}