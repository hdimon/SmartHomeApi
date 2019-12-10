using System.Collections.Generic;
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

            IList<Task<ISetValueResult>> commands;

            switch (args.NewValue)
            {
                case "Outdoor":
                    commands = GetOutdoorScenarioCommands();

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Indoor":
                    commands = GetIndoorScenarioCommands();

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Sleep":
                    commands = GetSleepScenarioCommands();

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Morning":
                    commands = GetMorningScenarioCommands();

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
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

        private IList<Task<ISetValueResult>> GetOutdoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "Off") };

            var currentToiletVentStatus = Manager.GetState("Toilet_Mega2560", "pin2");

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString() == "true")
                commands.Add(Manager.SetValue("Toilet_Mega2560", "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetIndoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = Manager.GetState("Toilet_Mega2560", "pin2");

            //Turn on toilet ventilator
            if (currentToiletVentStatus?.ToString() == "false")
                commands.Add(Manager.SetValue("Toilet_Mega2560", "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetSleepScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = Manager.GetState("Toilet_Mega2560", "pin2");

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString() == "true")
                commands.Add(Manager.SetValue("Toilet_Mega2560", "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetMorningScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            return commands;
        }
    }
}