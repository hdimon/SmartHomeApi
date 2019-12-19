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

        private const string KitchenMega2560 = "Kitchen_Mega2560";
        private const string KitchenSpotsLightPin = "pin2";
        private const string KitchenBalconySpotsLightPin = "pin4";
        private const string KitchenCentralLightPin = "pin5";
        private const string KitchenLedPin = "pin0";

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
                    commands = await GetOutdoorScenarioCommands().ConfigureAwait(false);

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Indoor":
                    commands = await GetIndoorScenarioCommands().ConfigureAwait(false);

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Sleep":
                    commands = await GetSleepScenarioCommands().ConfigureAwait(false);

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case "Morning":
                    commands = await GetMorningScenarioCommands().ConfigureAwait(false);

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
            }

            EnsureOperationIsSuccessful(args, results, _failoverActionIntervalSeconds, async () =>
            {
                var currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

                //If scenario has been already changed then stop
                if (currentScenario?.ToString() != args.NewValue)
                    return;

                await ProcessScenarioParameter(args).ConfigureAwait(false);
            });
        }

        private async Task<IList<Task<ISetValueResult>>> GetOutdoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            await TurnOffAllLighting(commands);

            return commands;
        }

        private async Task<IList<Task<ISetValueResult>>> GetIndoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenSpotsLightPin, FalseValue);
            //Hall, Bedroom

            return commands;
        }

        private async Task<IList<Task<ISetValueResult>>> GetSleepScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            await TurnOffAllLighting(commands).ConfigureAwait(false);

            return commands;
        }

        private async Task<IList<Task<ISetValueResult>>> GetMorningScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>>();

            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);

            return commands;
        }

        private async Task AddPositiveImpulseCommandWithStateCheck(IList<Task<ISetValueResult>> commands,
            string deviceId, string pin, string currentCheckValue)
        {
            var currentValue = await Manager.GetState(deviceId, pin).ConfigureAwait(false);

            if (currentValue?.ToString().ToLowerInvariant() == currentCheckValue)
                commands.Add(Manager.SetValue(deviceId, pin, "pimp"));
        }

        private async Task TurnOffAllLighting(IList<Task<ISetValueResult>> commands)
        {
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomSinkLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletSinkLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenSpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenBalconySpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenCentralLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenLedPin, TrueValue);
        }
    }
}