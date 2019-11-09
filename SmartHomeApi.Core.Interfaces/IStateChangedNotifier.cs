namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateChangedNotifier
    {
        void RegisterSubscriber(IStateChangedSubscriber subscriber);
        void UnregisterSubscriber(IStateChangedSubscriber subscriber);
        void NotifySubscribers(StateChangedEvent args);
    }
}