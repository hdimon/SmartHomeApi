﻿namespace SmartHomeApi.Core.Interfaces
{
    public interface IItemHelpersFabric
    {
        IItemStateStorageHelper GetItemStateStorageHelper();
        IJsonSerializer GetJsonSerializer();
        IApiLogger GetApiLogger();

        IDateTimeOffsetProvider GetDateTimeOffsetProvider();
        IItemState GetOrCreateItemState();

        IDynamicToObjectMapper GetDynamicToObjectMapper();
        IObjectToDynamicConverter GetObjectToDynamicConverter();
    }
}