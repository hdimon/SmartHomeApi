using System;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class DeviceHelpersDefaultFabric : IDeviceHelpersFabric
    {
        private readonly IServiceProvider _provider;

        public DeviceHelpersDefaultFabric(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IDeviceStateStorageHelper GetDeviceStateStorageHelper()
        {
            return _provider.GetService<IDeviceStateStorageHelper>();
        }

        public IApiLogger GetApiLogger()
        {
            return _provider.GetService<IApiLogger>();
        }
    }
}