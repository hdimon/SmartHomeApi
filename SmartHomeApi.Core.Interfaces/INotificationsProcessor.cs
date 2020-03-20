namespace SmartHomeApi.Core.Interfaces
{
    public interface INotificationsProcessor : IStateChangedNotifier
    {
        void NotifySubscribersAboutChanges(ApiManagerStateContainer oldStateContainer, ApiManagerStateContainer newStateContainer);
    }
}