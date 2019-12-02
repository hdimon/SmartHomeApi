using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public abstract class StateChangedSubscriberAbstract : IStateChangedSubscriber
    {
        protected readonly IApiManager Manager;
        protected readonly IItemHelpersFabric HelpersFabric;
        protected readonly IApiLogger Logger;

        protected StateChangedSubscriberAbstract(IApiManager manager, IItemHelpersFabric helpersFabric)
        {
            Manager = manager;
            HelpersFabric = helpersFabric;
            Logger = helpersFabric.GetApiLogger();

            Manager.RegisterSubscriber(this);
        }

        public async Task Notify(StateChangedEvent args)
        {
            await ProcessNotification(args);
        }

        protected abstract Task ProcessNotification(StateChangedEvent args);

        protected void EnsureOperationIsSuccessful(StateChangedEvent args, ISetValueResult[] results,
            int failoverActionIntervalSeconds, Func<Task> failoverAction)
        {
            if (results == null || results.All(r => r.Success))
                return;

            Logger.Info(
                $"Operation was not fully successful so try to execute it again in {failoverActionIntervalSeconds} seconds.");

            _ = Task.Run(async () =>
                    {
                        //Wait before trying to execute actions again
                        await Task.Delay(failoverActionIntervalSeconds * 1000).ConfigureAwait(false);

                        await failoverAction();
                    })
                    .ContinueWith(t => { Logger.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}