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

        public IDevicePluginLocator GetDevicePluginLocator()
        {
            return _provider.GetService<IDevicePluginLocator>();
        }

        public IDeviceLocator GetDeviceLocator()
        {
            return _provider.GetService<IDeviceLocator>();
        }

        public IDeviceConfigLocator GetDeviceConfigsLocator()
        {
            return _provider.GetService<IDeviceConfigLocator>();
        }

        public IRequestProcessor GetRequestProcessor()
        {
            return _provider.GetService<IRequestProcessor>();
        }

        public IDeviceManager GetDeviceManager()
        {
            return _provider.GetService<IDeviceManager>();
        }

        public IEventHandlerLocator GetEventHandlerLocator()
        {
            return _provider.GetService<IEventHandlerLocator>();
        }

        public IDeviceHelpersFabric GetDeviceHelpersFabric()
        {
            return _provider.GetService<IDeviceHelpersFabric>();
        }
    }
}