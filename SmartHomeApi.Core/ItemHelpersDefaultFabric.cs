using System;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class ItemHelpersDefaultFabric : IItemHelpersFabric
    {
        private readonly IServiceProvider _provider;

        public ItemHelpersDefaultFabric(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IItemStateStorageHelper GetDeviceStateStorageHelper()
        {
            return _provider.GetService<IItemStateStorageHelper>();
        }

        public IApiLogger GetApiLogger()
        {
            return _provider.GetService<IApiLogger>();
        }
    }
}