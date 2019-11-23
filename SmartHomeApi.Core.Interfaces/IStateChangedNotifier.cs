namespace SmartHomeApi.Core.Interfaces
{
    public interface IStateChangedNotifier : IItem
    {
        string ItemType { get; }
        string ItemId { get; }
        void RegisterSubscriber(IStateChangedSubscriber subscriber);
        void UnregisterSubscriber(IStateChangedSubscriber subscriber);
        void NotifySubscribers(StateChangedEvent args);
    }
}