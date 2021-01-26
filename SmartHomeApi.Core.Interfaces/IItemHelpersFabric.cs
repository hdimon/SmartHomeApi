namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemHelpersFabric
    {
        IItemStateStorageHelper GetItemStateStorageHelper();
        IJsonSerializer GetJsonSerializer();
        IApiLogger GetApiLogger();

        IDateTimeOffsetProvider GetDateTimeOffsetProvider();
        IItemStateNew GetOrCreateItemState();
    }
}