using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public abstract class StateChangedSubscriberAbstract : IStateChangedSubscriber
    {
        protected readonly IApiManager Manager;

        protected StateChangedSubscriberAbstract(IApiManager manager)
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