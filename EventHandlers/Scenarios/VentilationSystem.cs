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
            var breezartManualManagement = Manager.GetState("Virtual_States", "BreezartManualManagement");

            //If not parsed or manual management then return
            if (breezartManualManagement == null ||
                !bool.TryParse(breezartManualManagement.ToString(), out var value) || value)
                return;

            var recommendedSpeed = _ventilationSpeedManager.GetRecommendedSpeed();

            await Manager.SetValue(Breezart, "SetSpeed", recommendedSpeed.ToString(CultureInfo.InvariantCulture))
                         .ConfigureAwait(false);
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

            var currentToiletVentStatus = Manager.GetState(ToiletMega2560, "pin2");

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetIndoorScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = Manager.GetState(ToiletMega2560, "pin2");

            //Turn on toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "false")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetSleepScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

            var currentToiletVentStatus = Manager.GetState(ToiletMega2560, "pin2");

            //Turn off toilet ventilator
            if (currentToiletVentStatus?.ToString().ToLowerInvariant() == "true")
                commands.Add(Manager.SetValue(ToiletMega2560, "pin2", "pimp"));

            return commands;
        }

        private IList<Task<ISetValueResult>> GetMorningScenarioCommands()
        {
            var commands = new List<Task<ISetValueResult>> { Manager.SetValue("Breezart", "UnitState", "On") };

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