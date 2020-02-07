using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Utils;

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

        protected async Task ExecuteCommands(string itemType, IList<Task<ISetValueResult>> commands,
            StateChangedEvent args, int maxTries, int failoverActionIntervalSeconds, string errorMessage)
        {
            if (commands == null)
                return;

            try
            {
                await AsyncHelpers.RetryOnFault(async () =>
                    {
                        var results = await Task.WhenAll(commands).ConfigureAwait(false);

                        if (results == null || results.All(r => r.Success))
                            return;

                        Logger.Warning(
                            $"{itemType}. Operation was not fully successful so try to execute it again in " +
                            $"{failoverActionIntervalSeconds} seconds. Event: {args}.");

                        throw new Exception(
                            $"{itemType}. {errorMessage}. Event: {args}.");
                    }, maxTries,
                    () => Task.Delay(failoverActionIntervalSeconds * 1000)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}