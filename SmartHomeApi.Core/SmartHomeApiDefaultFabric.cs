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

        public IItemsConfigLocator GetItemsConfigsLocator()
        {
            return _provider.GetService<IItemsConfigLocator>();
        }

        public IApiManager GetApiManager()
        {
            return _provider.GetService<IApiManager>();
        }

        public IItemHelpersFabric GetItemHelpersFabric()
        {
            return _provider.GetService<IItemHelpersFabric>();
        }

        public IApiLogger GetApiLogger()
        {
            return _provider.GetService<IApiLogger>();
        }

        public IStatesContainerTransformer GetStateContainerTransformer()
        {
            return _provider.GetService<IStatesContainerTransformer>();
        }
    }
}