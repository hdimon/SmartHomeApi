namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateChangedNotifier : IItem
    {
        void RegisterSubscriber(IStateChangedSubscriber subscriber);
        void UnregisterSubscriber(IStateChangedSubscriber subscriber);
        void NotifySubscribers(StateChangedEvent args);
    }
}