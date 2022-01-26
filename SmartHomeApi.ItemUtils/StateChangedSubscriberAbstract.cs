using System;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.ItemUtils
{
    public abstract class StateChangedSubscriberAbstract : IStateChangedSubscriber, IAsyncDisposable
    {
        protected readonly IApiManager Manager;
        protected readonly IItemHelpersFabric HelpersFabric;
        protected readonly IApiLogger Logger;
        protected CancellationTokenSource DisposingCancellationTokenSource = new CancellationTokenSource();

        public virtual string ItemId { get; }
        public virtual string ItemType { get; }

        protected StateChangedSubscriberAbstract(IApiManager manager, IItemHelpersFabric helpersFabric)
        {
            Manager = manager;
            HelpersFabric = helpersFabric;
            Logger = helpersFabric.GetApiLogger();
        }

        public async Task Notify(StateChangedEvent args)
        {
            await ProcessNotification(args);
        }

        protected abstract Task ProcessNotification(StateChangedEvent args);

        public async ValueTask DisposeAsync()
        {
            try
            {
                Manager.UnregisterSubscriber(this);
                DisposingCancellationTokenSource.Cancel();

                await DisposeItem();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual ValueTask DisposeItem()
        {
            return ValueTask.CompletedTask;
        }
    }
}