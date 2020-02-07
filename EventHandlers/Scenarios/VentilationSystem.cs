using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class VentilationSystem : StateChangedSubscriberAbstract
    {
        private readonly int _failoverActionIntervalSeconds = 30;
        private readonly int _failoverMaxTries = 3;
        private readonly VentilationSpeedManager _ventilationSpeedManager;
        private Task _worker;
        private TimeSpan _measurementPeriod = new TimeSpan(0, 0, 5, 0);
        private bool _firstWorkerRun = true;
        private int _firstWorkerRunDelaySeconds = 10;

        private const string ToiletMega2560 = "Toilet_Mega2560";
        private const string BedroomMega2560 = "Bedroom_Mega2560";
        private const string KitchenMega2560 = "Kitchen_Mega2560";
        private const string LivingRoomMega2560 = "LivingRoom_Mega2560";
        private const string Breezart = "Breezart";

        public VentilationSystem(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager, helpersFabric)
        {
            _ventilationSpeedManager = new VentilationSpeedManager(_measurementPeriod);

            RunVentilationSpeedManagerWorker();
        }

        private void RunVentilationSpeedManagerWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(VentilationSpeedManagerWorkerWrapper).Unwrap().ContinueWith(
                    t =>
                    {
                        Logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task VentilationSpeedManagerWorkerWrapper()
        {
            while (true)
            {
                if (!_firstWorkerRun)
                    await Task.Delay(_measurementPeriod).ConfigureAwait(false);
                else
                    await Task.Delay(_firstWorkerRunDelaySeconds * 1000).ConfigureAwait(false); //Wait a bit before first run

                _firstWorkerRun = false;

                try
                {
                    await VentilationSpeedManagerWorker().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private async Task VentilationSpeedManagerWorker()
        {
            var breezartManualManagement =
                await Manager.GetState("Virtual_States", "BreezartManualManagement").ConfigureAwait(false);

            //If not parsed or manual management then return
            if (breezartManualManagement == null ||
                !bool.TryParse(breezartManualManagement.ToString(), out var value) || value)
                return;

            var recommendedSpeed = _ventilationSpeedManager.GetRecommendedSpeed();

            var currentSetSpeedObj = await Manager.GetState(Breezart, "SetSpeed").ConfigureAwait(false);

            var parseResult = int.TryParse(currentSetSpeedObj.ToString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var currentSetSpeed);

            if (!parseResult || recommendedSpeed != currentSetSpeed)
            {
                Logger.Info(
                    $"Recommended ventilation speed has been changed from {currentSetSpeed} to {recommendedSpeed}");

                await Manager.SetValue(Breezart, "SetSpeed", recommendedSpeed.ToString(CultureInfo.InvariantCulture))
                             .ConfigureAwait(false);
            }
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
                case BedroomMega2560:
                    await ProcessBedroomMega2560StateEvents(args).ConfigureAwait(false);
                    break;
                case KitchenMega2560:
                    await ProcessKitchenMega2560StateEvents(args).ConfigureAwait(false);
                    break;
                case LivingRoomMega2560:
                    await ProcessLivingRoomMega2560StateEvents(args).ConfigureAwait(false);
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
            IList<Task<ISetValueResult>> commands = null;

            switch (args.NewValue)
            {
                case "Outdoor":
                    commands = await GetOutdoorScenarioCommands().ConfigureAwait(false);
                    break;
                case "Indoor":
                    commands = await GetIndoorScenarioCommands().ConfigureAwait(false);
                    break;
                case "Sleep":
                    commands = await GetSleepScenarioCommands().ConfigureAwait(false);
                    break;
            }

            await ExecuteCommands(nameof(VentilationSystem), commands, args, _failoverMaxTries,
                _failoverActionIntervalSeconds, "Some Scenario commands were failed").ConfigureAwait(false);
        }

        private async Task<IList<Task<ISetValueResult>>> GetOutdoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "Off") };

            var currentToiletVentStatus = await Manager.GetState(ToiletMega2560, "pin2").ConfigureAwait(false);

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            var currentKitchenVentStatus = await Manager.GetState(KitchenMega2560, "pin3").ConfigureAwait(false);

            //Turn off kitchen ventilator
            if (currentKitchenVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(KitchenMega2560, "pin3", "pimp"));

            return commands;
        }

        private async Task<IList<Task<ISetValueResult>>> GetIndoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = await Manager.GetState(ToiletMega2560, "pin2").ConfigureAwait(false);

            //Turn on toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "false")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            return commands;
        }

        private async Task<IList<Task<ISetValueResult>>> GetSleepScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = await Manager.GetState(ToiletMega2560, "pin2").ConfigureAwait(false);

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            var currentKitchenVentStatus = await Manager.GetState(KitchenMega2560, "pin3").ConfigureAwait(false);

            //Turn off kitchen ventilator
            if (currentKitchenVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(KitchenMega2560, "pin3", "pimp"));

            return commands;
        }

        private async Task ProcessBedroomMega2560StateEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "CO2ppm":
                    await ProcessCO2Parameter(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessKitchenMega2560StateEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "CO2ppm":
                    await ProcessCO2Parameter(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessLivingRoomMega2560StateEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "CO2ppm":
                    await ProcessCO2Parameter(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessCO2Parameter(StateChangedEvent args)
        {
            try
            {
                _ventilationSpeedManager.AddEvent(args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}