using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class LightingSystem : StateChangedSubscriberAbstract
    {
        private readonly int _failoverActionIntervalSeconds = 30;

        private const string TrueValue = "true";
        private const string FalseValue = "false";
        private const string ToiletMega2560 = "Toilet_Mega2560";
        private const string BathroomLightPin = "pin6";
        private const string BathroomSinkLightPin = "pin4";
        private const string ToiletLightPin = "pin5";
        private const string ToiletSinkLightPin = "pin7";

        public LightingSystem(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager, helpersFabric)
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
            var commands = new List<Task<ISetValueResult>>();

            TurnOffAllLighting(commands);

            return commands;
        }

        private IList<Task<ISetValueResult>> GetIndoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);
            //+Kitchen, Hall, Bedroom

            return commands;
        }

        private IList<Task<ISetValueResult>> GetSleepScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            TurnOffAllLighting(commands);

            return commands;
        }

        private IList<Task<ISetValueResult>> GetMorningScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);

            return commands;
        }

        private void AddPositiveImpulseCommandWithStateCheck(IList<Task<ISetValueResult>> commands, string deviceId,
            string pin, string currentCheckValue)
        {
            var currentValue = Manager.GetState(deviceId, pin);

            if (currentValue?.ToString() == currentCheckValue)
                commands.Add(Manager.SetValue(deviceId, pin, "pimp"));
        }

        private void TurnOffAllLighting(IList<Task<ISetValueResult>> commands)
        {
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, TrueValue);
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomSinkLightPin, TrueValue);
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, TrueValue);
            AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletSinkLightPin, TrueValue);
        }
    }
}