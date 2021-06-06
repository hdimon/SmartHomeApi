using System;

namespace SmartHomeApi.Core.Interfaces
{
    public interface INotificationsProcessor : IStateChangedNotifier, IAsyncDisposable
    {
    }
}