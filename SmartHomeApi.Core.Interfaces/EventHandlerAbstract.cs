using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public abstract class EventHandlerAbstract : IEventHandler
    {
        protected readonly IDeviceManager Manager;

        protected EventHandlerAbstract(IDeviceManager manager)
        {
            Manager = manager;

            Manager.RegisterSubscriber(this);
        }

        public async Task Notify(StateChangedEvent args)
        {
            await ProcessNotification(args);
        }

        protected abstract Task ProcessNotification(StateChangedEvent args);
    }
}