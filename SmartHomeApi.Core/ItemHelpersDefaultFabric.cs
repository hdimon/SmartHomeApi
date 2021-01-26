using System;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Services;

namespace SmartHomeApi.Core
{
    public class ItemHelpersDefaultFabric : IItemHelpersFabric
    {
        private readonly IServiceProvider _provider;
        private readonly string _itemId;
        private readonly string _itemType;

        public ItemHelpersDefaultFabric(IServiceProvider provider, string itemId)
        {
            _provider = provider;
            _itemId = itemId;
        }

        public ItemHelpersDefaultFabric(IServiceProvider provider, string itemId, string itemType)
        {
            _provider = provider;
            _itemId = itemId;
            _itemType = itemType;
        }

        public IItemStateStorageHelper GetItemStateStorageHelper()
        {
            return _provider.GetService<IItemStateStorageHelper>();
        }

        public IJsonSerializer GetJsonSerializer()
        {
            return _provider.GetService<IJsonSerializer>();
        }

        public IApiLogger GetApiLogger()
        {
            var logger = _provider.GetService<IApiLogger>();

            return new ApiItemLogger(logger, _itemId);
        }

        public IDateTimeOffsetProvider GetDateTimeOffsetProvider()
        {
            return _provider.GetService<IDateTimeOffsetProvider>();
        }

        public IItemStateNew GetOrCreateItemState()
        {
            var statesProcessor = _provider.GetService<IItemStatesProcessor>();

            return statesProcessor.GetOrCreateItemState(_itemId, _itemType);
        }
    }
}