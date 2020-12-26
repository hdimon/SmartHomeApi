﻿using System;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Services;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class SmartHomeApiStubFabric : ISmartHomeApiFabric
    {
        public AppSettings GetConfiguration()
        {
            return new AppSettings();
        }

        public IItemsPluginsLocator GetItemsPluginsLocator()
        {
            throw new NotImplementedException();
        }

        public IItemsLocator GetItemsLocator()
        {
            throw new NotImplementedException();
        }

        public IItemsConfigLocator GetItemsConfigsLocator()
        {
            throw new NotImplementedException();
        }

        public IApiManager GetApiManager()
        {
            throw new NotImplementedException();
        }

        public IItemHelpersFabric GetItemHelpersFabric(string itemId)
        {
            throw new NotImplementedException();
        }

        public IItemHelpersFabric GetItemHelpersFabric(string itemId, string itemType)
        {
            throw new NotImplementedException();
        }

        public IApiLogger GetApiLogger()
        {
            return new ApiStubLogger();
        }

        public INotificationsProcessor GetNotificationsProcessor()
        {
            return new StubNotificationsProcessor();
        }

        public IUntrackedStatesProcessor GetUntrackedStatesProcessor()
        {
            throw new NotImplementedException();
        }

        public IUncachedStatesProcessor GetUncachedStatesProcessor()
        {
            throw new NotImplementedException();
        }
    }
}