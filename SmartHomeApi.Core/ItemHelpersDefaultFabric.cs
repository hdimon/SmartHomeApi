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

        public ItemHelpersDefaultFabric(IServiceProvider provider, string itemId)
        {
            _provider = provider;
            _itemId = itemId;
        }

        public IItemStateStorageHelper GetDeviceStateStorageHelper()
        {
            return _provider.GetService<IItemStateStorageHelper>();
        }

        public IApiLogger GetApiLogger()
        {
            var logger = _provider.GetService<IApiLogger>();

            return new ApiItemLogger(logger, _itemId);
        }
    }
}