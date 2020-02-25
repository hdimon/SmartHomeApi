using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class LightingSystem : StateChangedSubscriberAbstract
    {
        private readonly int _failoverActionIntervalSeconds = 30;
        private readonly int _failoverMaxTries = 6;

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

        private const string BedroomMega2560 = "Bedroom_Mega2560";
        private const string BedroomSpotsLightPin = "pin6";
        private const string BedroomBalconySpotsLightPin = "pin4";
        private const string BedroomCentralLightPin = "pin7";
        private const string HallSpotsLightPin = "pin5";
        private const string HallSconcesPin = "pin3";

        public LightingSystem(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager, helpersFabric)
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            if (args == null)
                return;

            if (args.EventType == StateChangedEventType.ValueSet)
                return;

            switch (args.ItemId)
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
            IList<SetValueCommand> commands = null;

            switch (args.NewValue)
            {
                case "Outdoor":
                    commands = await GetOutdoorScenarioCommands().ConfigureAwait(false);
                    break;
                case "Indoor":
                    commands = await GetIndoorScenarioCommands(args).ConfigureAwait(false);
                    break;
                case "Sleep":
                    commands = await GetSleepScenarioCommands().ConfigureAwait(false);
                    break;
            }

            await ExecuteCommands(nameof(LightingSystem), commands, args, _failoverMaxTries,
                    _failoverActionIntervalSeconds, "Some Scenario commands were failed", CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async Task<IList<SetValueCommand>> GetOutdoorScenarioCommands()
        {
            var commands = new List<SetValueCommand>();

            await TurnOffAllLighting(commands).ConfigureAwait(false);

            return commands;
        }

        private async Task<IList<SetValueCommand>> GetIndoorScenarioCommands(StateChangedEvent args)
        {
            var commands = new List<SetValueCommand>();

            if (args.OldValue == "Outdoor")
            {
                await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
                await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);
                await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenSpotsLightPin,
                    FalseValue);
                await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, BedroomSpotsLightPin,
                    FalseValue);
                await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, HallSpotsLightPin, FalseValue);
            }
            else if (args.OldValue == "Sleep")
            {
                await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, FalseValue);
                await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, FalseValue);
            }

            return commands;
        }

        private async Task<IList<SetValueCommand>> GetSleepScenarioCommands()
        {
            var commands = new List<SetValueCommand>();

            await TurnOffAllLighting(commands).ConfigureAwait(false);

            return commands;
        }

        private async Task AddPositiveImpulseCommandWithStateCheck(IList<SetValueCommand> commands,
            string deviceId, string pin, string currentCheckValue)
        {
            var currentValue = await Manager.GetState(deviceId, pin).ConfigureAwait(false);

            if (currentValue?.ToString().ToLowerInvariant() == currentCheckValue)
                commands.Add(CreateCommand(deviceId, pin, "pimp"));
        }

        private async Task TurnOffAllLighting(IList<SetValueCommand> commands)
        {
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, BathroomSinkLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, ToiletMega2560, ToiletSinkLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenSpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenBalconySpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenCentralLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, KitchenMega2560, KitchenLedPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, HallSpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, HallSconcesPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, BedroomSpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, BedroomBalconySpotsLightPin, TrueValue);
            await AddPositiveImpulseCommandWithStateCheck(commands, BedroomMega2560, BedroomCentralLightPin, TrueValue);
        }
    }
}