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

        [Obsolete("Please override DisposeItem() method and move code from your Dispose() implementation " +
                  "into DisposeItem(). Don't move base.Dispose() line if it's called in your Dispose() method.")]
        public virtual void Dispose()
        {
            //Make it now empty but leave it here because plugins overrides it
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                Manager.UnregisterSubscriber(this);
                DisposingCancellationTokenSource.Cancel();

                await DisposeItem();
                Dispose(); //Call it here for backward compatibility
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