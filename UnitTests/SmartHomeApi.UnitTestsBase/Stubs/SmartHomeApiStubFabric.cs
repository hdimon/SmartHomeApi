﻿using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class SmartHomeApiStubFabric : ISmartHomeApiFabric
    {
        private readonly AppSettings _appSettings;
        private readonly IApiLogger _logger;

        public IItemsPluginsLocator ItemsPluginsLocator { get; set; }
        public IItemsConfigLocator ItemsConfigLocator { get; set; }

        public SmartHomeApiStubFabric()
        {
            _logger = new ApiStubLogger();
        }

        public SmartHomeApiStubFabric(AppSettings appSettings) : this()
        {
            _appSettings = appSettings;
        }

        public AppSettings GetConfiguration()
        {
            return _appSettings ?? new AppSettings();
        }

        public IItemsPluginsLocator GetItemsPluginsLocator()
        {
            return ItemsPluginsLocator;
        }

        public IItemsLocator GetItemsLocator()
        {
            throw new NotImplementedException();
        }

        public IApiItemsLocator GetApiItemsLocator()
        {
            throw new NotImplementedException();
        }

        public IItemsConfigLocator GetItemsConfigsLocator()
        {
            return ItemsConfigLocator;
        }

        public IApiManager GetApiManager()
        {
            return new ApiManagerStub();
        }

        public IItemHelpersFabric GetItemHelpersFabric(string itemId, string itemType)
        {
            return new ItemHelpersStubFabric(itemId, itemType, this);
        }

        public IApiLogger GetApiLogger()
        {
            return _logger;
        }

        public IDateTimeOffsetProvider GetDateTimeOffsetProvider()
        {
            return new FakeDateTimeOffsetProvider();
        }

        public INotificationsProcessor GetNotificationsProcessor()
        {
            return new StubNotificationsProcessor();
        }

        public IUncachedStatesProcessor GetUncachedStatesProcessor()
        {
            throw new NotImplementedException();
        }

        public IDynamicToObjectMapper GetDynamicToObjectMapper()
        {
            throw new NotImplementedException();
        }

        public IObjectToDynamicConverter GetObjectToDynamicConverter()
        {
            throw new NotImplementedException();
        }
    }
}