using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.Core
{
    public class SmartHomeApiDefaultFabric : ISmartHomeApiFabric
    {
        private readonly IServiceProvider _provider;

        public SmartHomeApiDefaultFabric(IServiceProvider provider)
        {
            _provider = provider;
        }

        public AppSettings GetConfiguration()
        {
            var config = _provider.GetService<IOptionsMonitor<AppSettings>>().CurrentValue;

            //Since CurrentValue contains reference to the same object till appsettings.json changed
            //then make copy of object to make sure it will not be corrupted anywhere in code

            return (AppSettings)config.Clone();
        }

        public IItemsPluginsLocator GetItemsPluginsLocator()
        {
            return _provider.GetService<IItemsPluginsLocator>();
        }

        public IItemsLocator GetItemsLocator()
        {
            return _provider.GetService<IItemsLocator>();
        }

        public IApiItemsLocator GetApiItemsLocator()
        {
            return _provider.GetService<IApiItemsLocator>();
        }

        public IItemsConfigLocator GetItemsConfigsLocator()
        {
            return _provider.GetService<IItemsConfigLocator>();
        }

        public IApiManager GetApiManager()
        {
            return _provider.GetService<IApiManager>();
        }

        public IItemHelpersFabric GetItemHelpersFabric(string itemId)
        {
            return new ItemHelpersDefaultFabric(_provider, itemId);
            //return _provider.GetService<IItemHelpersFabric>();
        }

        public IItemHelpersFabric GetItemHelpersFabric(string itemId, string itemType)
        {
            return new ItemHelpersDefaultFabric(_provider, itemId, itemType);
        }

        public IApiLogger GetApiLogger()
        {
            return _provider.GetService<IApiLogger>();
        }

        public IDateTimeOffsetProvider GetDateTimeOffsetProvider()
        {
            return _provider.GetService<IDateTimeOffsetProvider>();
        }

        public INotificationsProcessor GetNotificationsProcessor()
        {
            return _provider.GetService<INotificationsProcessor>();
        }

        public IUncachedStatesProcessor GetUncachedStatesProcessor()
        {
            return _provider.GetService<IUncachedStatesProcessor>();
        }
    }
}