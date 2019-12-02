using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class VentilationSystem : StateChangedSubscriberAbstract
    {
        private readonly int _failoverActionIntervalSeconds = 30;

        public VentilationSystem(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager, helpersFabric)
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            if (args == null)
                return;

            if (args.EventType == StateChangedEventType.ValueSet)
                return;

            switch (args.DeviceId)
            {
                case "Virtual_States":
                    await ProcessVirtualStateEvents(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessVirtualStateEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "Scenario":
                    await ProcessScenarioParameter(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessScenarioParameter(StateChangedEvent args)
        {
            ISetValueResult[] results = null;

            switch (args.NewValue)
            {
                case "Outdoor":
                    results = await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "Off"))
                                        .ConfigureAwait(false);
                    break;
                case "Indoor":
                    results = await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "On")).ConfigureAwait(false);
                    break;
                case "Sleep":
                    results = await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "On")).ConfigureAwait(false);
                    break;
                case "Morning":
                    results = await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "On")).ConfigureAwait(false);
                    break;
            }

            EnsureOperationIsSuccessful(args, results, _failoverActionIntervalSeconds, async () =>
            {
                var currentScenario = Manager.GetState("Virtual_States", "Scenario");

                //If scenario has been already changed then stop
                if (currentScenario?.ToString() != args.NewValue)
                    return;

                await ProcessScenarioParameter(args).ConfigureAwait(false);
            });
        }
    }
}