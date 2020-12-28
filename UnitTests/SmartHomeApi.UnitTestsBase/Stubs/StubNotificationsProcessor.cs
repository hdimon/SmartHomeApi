using System;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.UnitTestsBase.Stubs
{
    public class StubNotificationsProcessor : INotificationsProcessor
    {
        public string ItemType => null;
        public string ItemId => null;

        public void RegisterSubscriber(IStateChangedSubscriber subscriber)
        {
            throw new NotImplementedException();
        }

        public void UnregisterSubscriber(IStateChangedSubscriber subscriber)
        {
            throw new NotImplementedException();
        }

        public void NotifySubscribers(StateChangedEvent args)
        {
        }
    }
}