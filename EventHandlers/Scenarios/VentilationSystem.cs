using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class VentilationSystem : StateChangedSubscriberAbstract
    {
        public VentilationSystem(IApiManager manager) : base(manager)
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
            switch (args.NewValue)
            {
                case "Outdoor":
                    await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "Off")).ConfigureAwait(false);
                    break;
                case "Indoor":
                    await Task.WhenAll(Manager.SetValue("Breezart", "UnitState", "On")).ConfigureAwait(false);
                    break;
                case "Sleep":
                    break;
                case "Morning":
                    break;
            }
        }
    }
}