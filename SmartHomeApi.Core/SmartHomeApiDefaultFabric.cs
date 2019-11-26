using System;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class SmartHomeApiDefaultFabric : ISmartHomeApiFabric
    {
        private readonly IServiceProvider _provider;

        public SmartHomeApiDefaultFabric(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IItemsPluginsLocator GetItemsPluginsLocator()
        {
            return _provider.GetService<IItemsPluginsLocator>();
        }

        public IItemsLocator GetItemsLocator()
        {
            return _provider.GetService<IItemsLocator>();
        }

        public IDeviceConfigLocator GetDeviceConfigsLocator()
        {
            return _provider.GetService<IDeviceConfigLocator>();
        }

        public IApiManager GetApiManager()
        {
            return _provider.GetService<IApiManager>();
        }

        public IDeviceHelpersFabric GetDeviceHelpersFabric()
        {
            return _provider.GetService<IDeviceHelpersFabric>();
        }

        public IApiLogger GetApiLogger()
        {
            return _provider.GetService<IApiLogger>();
        }
    }
}