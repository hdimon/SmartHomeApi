﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    class ApiManagerStub : IApiManager
    {
        public bool IsInitialized { get; }
        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public string ItemType { get; }
        public string ItemId { get; }
        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
            throw new NotImplementedException();
        }

        public Task<ISetValueResult> SetValue(string itemId, string parameter, object value)
        {
            throw new NotImplementedException();
        }

        public Task<ISetValueResult> Increase(string itemId, string parameter)
        {
            throw new NotImplementedException();
        }

        public Task<ISetValueResult> Decrease(string itemId, string parameter)
        {
            throw new NotImplementedException();
        }

        public Task<IStatesContainer> GetState()
        {
            throw new NotImplementedException();
        }

        public Task<IItemStateModel> GetState(string itemId)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetState(string itemId, string parameter)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IItem>> GetItems()
        {
            throw new NotImplementedException();
        }

        public Task<object> Execute(string itemId, string command, dynamic data, Type resultType)
        {
            throw new NotImplementedException();
        }
    }
}
