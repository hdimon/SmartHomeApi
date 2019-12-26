using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class HeatingSystem : StateChangedSubscriberAbstract
    {
        private readonly int _heatingSystemMorningAdvanceMinutes = 45;
        private readonly int _heatingSystemMorningDurationMinutes = 60;
        private readonly int _towelHeaterDurationMinutes = 60;
        private readonly int _failoverActionIntervalSeconds = 30;
        private const string _indoorScenario = "Indoor";
        private const string _outdoorSubScenarioGoingHome = "GoingHome";
        private const string _outdoorSubScenarioNone = "None";
        private const string _outdoorScenario = "Outdoor";
        private const string _sleepScenario = "Sleep";
        private const string _indoorSubScenarioHeatingFlat = "HeatingFlat";
        private const string _indoorSubScenarioNone = "None";

        public HeatingSystem(IApiManager manager, IItemHelpersFabric helpersFabric) : base(manager, helpersFabric)
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
                case "Virtual_MainAlarmClock":
                    await ProcessVirtualMainAlarmClockEvents(args).ConfigureAwait(false);
                    break;
                case "Virtual_HeatingSystemMorningAlarmClock":
                    await ProcessVirtualHeatingSystemMorningAlarmClockEvents(args).ConfigureAwait(false);
                    break;
                case "Virtual_HeatingSystemAfterMorningAlarmClock":
                    await ProcessVirtualHeatingSystemAfterMorningAlarmClockEvents(args).ConfigureAwait(false);
                    break;
                case "Virtual_TowelHeaterTurningOffAlarmClock":
                    await ProcessVirtualTowelHeaterTurningOffAlarmClockEvents(args).ConfigureAwait(false);
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
                case "OutdoorSubScenario":
                    await ProcessOutdoorSubScenarioParameter(args).ConfigureAwait(false);
                    break;
                case "IndoorSubScenario":
                    await ProcessIndoorSubScenarioParameter(args).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessScenarioParameter(StateChangedEvent args)
        {
            ISetValueResult[] results = null;

            DateTime towelHeaterAlarm;
            string towelHeaterAlarmStr;

            List<Task<ISetValueResult>> commands;
            switch (args.NewValue)
            {
                case _outdoorScenario:
                    towelHeaterAlarm = DateTime.Now.AddMinutes(_towelHeaterDurationMinutes);
                    towelHeaterAlarmStr = towelHeaterAlarm.ToLongTimeString();

                    commands = new List<Task<ISetValueResult>>
                    {
                        Manager.SetValue("Virtual_States", "OutdoorSubScenario", _outdoorSubScenarioNone),
                        Manager.SetValue("Virtual_States", "IndoorSubScenario", _indoorSubScenarioNone)
                    };
                    commands.AddRange(GetOutdoorScenarioTemperatureCommands());
                    commands.Add(Manager.SetValue("Virtual_TowelHeaterTurningOffAlarmClock", "Time",
                        towelHeaterAlarmStr));

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case _indoorScenario:
                    commands = new List<Task<ISetValueResult>>
                        { Manager.SetValue("Virtual_States", "OutdoorSubScenario", _outdoorSubScenarioNone) };

                    var indoorSubScenario =
                        await Manager.GetState("Virtual_States", "IndoorSubScenario").ConfigureAwait(false);

                    if (indoorSubScenario?.ToString() == _indoorSubScenarioNone)
                        commands.AddRange(GetIndoorScenarioTemperatureCommands());

                    commands.Add(Manager.SetValue("Toilet_Mega2560", "pin3", "low"));

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case _sleepScenario:
                    towelHeaterAlarm = DateTime.Now.AddMinutes(_towelHeaterDurationMinutes);
                    towelHeaterAlarmStr = towelHeaterAlarm.ToLongTimeString();

                    commands = new List<Task<ISetValueResult>>
                    {
                        Manager.SetValue("Virtual_States", "OutdoorSubScenario", _outdoorSubScenarioNone),
                        Manager.SetValue("Virtual_States", "IndoorSubScenario", _indoorSubScenarioNone)
                    };
                    commands.AddRange(GetSleepScenarioTemperatureCommands());
                    commands.Add(Manager.SetValue("Virtual_TowelHeaterTurningOffAlarmClock", "Time",
                        towelHeaterAlarmStr));

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

        private IList<Task<ISetValueResult>> GetOutdoorScenarioTemperatureCommands()
        {
            var commands = new List<Task<ISetValueResult>>
            {
                Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                Manager.SetValue("Bedroom_Floor", "SetTemperature", "20")
            };

            return commands;
        }

        private IList<Task<ISetValueResult>> GetIndoorScenarioTemperatureCommands()
        {
            var commands = new List<Task<ISetValueResult>>
            {
                Manager.SetValue("Kitchen_Floor", "SetTemperature", "29"),
                Manager.SetValue("Bathroom_Floor", "SetTemperature", "28"),
                Manager.SetValue("Toilet_Floor", "SetTemperature", "28"),
                Manager.SetValue("Bedroom_Floor", "SetTemperature", "26")
            };

            return commands;
        }

        private IList<Task<ISetValueResult>> GetSleepScenarioTemperatureCommands()
        {
            var commands = new List<Task<ISetValueResult>>
            {
                Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                Manager.SetValue("Bedroom_Floor", "SetTemperature", "20")
            };

            return commands;
        }

        private IList<Task<ISetValueResult>> GetIndoorHeatingFlatSubScenarioTemperatureCommands()
        {
            var commands = new List<Task<ISetValueResult>>
            {
                Manager.SetValue("Kitchen_Floor", "SetTemperature", "30"),
                Manager.SetValue("Bathroom_Floor", "SetTemperature", "30"),
                Manager.SetValue("Toilet_Floor", "SetTemperature", "30"),
                Manager.SetValue("Bedroom_Floor", "SetTemperature", "26")
            };

            return commands;
        }

        private async Task ProcessOutdoorSubScenarioParameter(StateChangedEvent args)
        {
            ISetValueResult[] results = null;

            IList<Task<ISetValueResult>> commands;

            var currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

            switch (args.NewValue)
            {
                case _outdoorSubScenarioGoingHome:
                    if (currentScenario?.ToString() != _outdoorScenario)
                        return;

                    commands = GetIndoorScenarioTemperatureCommands();
                    commands.Add(Manager.SetValue("Toilet_Mega2560", "pin3", "low"));
                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case _outdoorSubScenarioNone:
                    if (currentScenario?.ToString() != _outdoorScenario)
                        return;

                    commands = GetOutdoorScenarioTemperatureCommands();
                    commands.Add(Manager.SetValue("Toilet_Mega2560", "pin3", "high"));
                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
            }

            EnsureOperationIsSuccessful(args, results, _failoverActionIntervalSeconds, async () =>
            {
                currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

                //If scenario not outdoor then stop
                if (currentScenario?.ToString() != _outdoorScenario)
                    return;

                await ProcessOutdoorSubScenarioParameter(args).ConfigureAwait(false);
            });
        }

        private async Task ProcessIndoorSubScenarioParameter(StateChangedEvent args)
        {
            ISetValueResult[] results = null;

            IList<Task<ISetValueResult>> commands;

            var currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

            switch (args.NewValue)
            {
                case _indoorSubScenarioHeatingFlat:
                    if (currentScenario?.ToString() != _indoorScenario && currentScenario?.ToString() != _sleepScenario)
                        return;

                    commands = GetIndoorHeatingFlatSubScenarioTemperatureCommands();
                    //commands.Add(Manager.SetValue("Toilet_Mega2560", "pin3", "low"));
                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
                case _indoorSubScenarioNone:
                    if (currentScenario?.ToString() != _indoorScenario && currentScenario?.ToString() != _sleepScenario)
                        return;

                    if (currentScenario.ToString() == _indoorScenario)
                        commands = GetIndoorScenarioTemperatureCommands();
                    else if(currentScenario.ToString() == _sleepScenario)
                        commands = GetSleepScenarioTemperatureCommands();
                    else
                    {
                        commands = new List<Task<ISetValueResult>>();
                    }

                    results = await Task.WhenAll(commands).ConfigureAwait(false);
                    break;
            }

            EnsureOperationIsSuccessful(args, results, _failoverActionIntervalSeconds, async () =>
            {
                currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

                //If scenario not indoor or sleep then stop
                if (currentScenario?.ToString() != _indoorScenario && currentScenario?.ToString() != _sleepScenario)
                    return;

                await ProcessIndoorSubScenarioParameter(args).ConfigureAwait(false);
            });
        }

        private async Task ProcessVirtualMainAlarmClockEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "NextAlarmDateTime":
                    var mainAlarmTimeStr = args.NewValue;
                    var mainAlarmTime = DateTime.Parse(mainAlarmTimeStr);
                    var heatingSystemAlarm = mainAlarmTime.AddMinutes(-_heatingSystemMorningAdvanceMinutes);
                    var heatingSystemAlarmStr = heatingSystemAlarm.ToLongTimeString();

                    await Manager.SetValue("Virtual_HeatingSystemMorningAlarmClock", "Time", heatingSystemAlarmStr)
                                 .ConfigureAwait(false);
                    break;
                case "Enabled":
                    if (!bool.TryParse(args.NewValue, out bool _))
                        return;

                    await Manager.SetValue("Virtual_HeatingSystemMorningAlarmClock", "Enabled", args.NewValue)
                                 .ConfigureAwait(false);
                    await Manager.SetValue("Virtual_HeatingSystemAfterMorningAlarmClock", "Enabled", args.NewValue)
                                 .ConfigureAwait(false);
                    break;
                case "Alarm":
                    await Manager.SetValue("Virtual_States", "Scenario", _indoorScenario).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessVirtualHeatingSystemMorningAlarmClockEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "Alarm":
                    if (args.EventType == StateChangedEventType.ValueRemoved)
                        break;

                    var currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);

                    //1. If scenario is already indoor it means alarm clock was reset
                    //(for example initially it was set on 8:30 but in 8:20 or 8:40 (i.e. during Morning interval) it was reset to 8:50)
                    //or it means daytime sleep
                    //then just cancel alarm.
                    //2. If scenario is Outdoor it means nobody is in home so just cancel alarm, no need to heat flat.
                    if (currentScenario?.ToString() == _outdoorScenario ||
                        currentScenario?.ToString() == _indoorScenario)
                    {
                        await Manager.SetValue("Virtual_HeatingSystemMorningAlarmClock", "Alarm", null)
                                     .ConfigureAwait(false);
                        break;
                    }

                    var alarmTimeStr = args.NewValue;
                    var alarmTime = DateTime.Parse(alarmTimeStr);
                    var afterMorningAlarmTime = alarmTime
                                                .AddMinutes(_heatingSystemMorningAdvanceMinutes)
                                                .AddMinutes(_heatingSystemMorningDurationMinutes);
                    var afterMorningAlarmTimeStr = afterMorningAlarmTime.ToLongTimeString();

                    await Manager.SetValue("Virtual_States", "IndoorSubScenario", _indoorSubScenarioHeatingFlat)
                                 .ConfigureAwait(false);
                    await Manager.SetValue("Virtual_HeatingSystemMorningAlarmClock", "Alarm", null)
                                 .ConfigureAwait(false);
                    await Manager
                          .SetValue("Virtual_HeatingSystemAfterMorningAlarmClock", "Time", afterMorningAlarmTimeStr)
                          .ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessVirtualHeatingSystemAfterMorningAlarmClockEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "Alarm":
                    await Manager.SetValue("Virtual_States", "IndoorSubScenario", _indoorSubScenarioNone)
                                 .ConfigureAwait(false);
                    await Manager.SetValue("Virtual_HeatingSystemAfterMorningAlarmClock", "Alarm", null)
                                 .ConfigureAwait(false);
                    break;
            }
        }
        private async Task ProcessVirtualTowelHeaterTurningOffAlarmClockEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "Alarm":
                    await Manager.SetValue("Virtual_TowelHeaterTurningOffAlarmClock", "Alarm", null)
                                 .ConfigureAwait(false);

                    var currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);
                    var currentOutdoorSubScenario =
                        await Manager.GetState("Virtual_States", "OutdoorSubScenario").ConfigureAwait(false);

                    //If scenario is Indoor or Outdoor but already going home then no need to turn off
                    if (currentScenario?.ToString() == _indoorScenario ||
                        currentScenario?.ToString() == _outdoorScenario &&
                        currentOutdoorSubScenario?.ToString() == _outdoorSubScenarioGoingHome)
                        return;

                    //Turn off TowelHeater
                    var results = await Task.WhenAll(Manager.SetValue("Toilet_Mega2560", "pin3", "high"))
                                            .ConfigureAwait(false);

                    EnsureOperationIsSuccessful(args, results, _failoverActionIntervalSeconds, async () =>
                    {
                        currentScenario = await Manager.GetState("Virtual_States", "Scenario").ConfigureAwait(false);
                        currentOutdoorSubScenario =
                            await Manager.GetState("Virtual_States", "OutdoorSubScenario").ConfigureAwait(false);

                        //If scenario is Indoor or Outdoor but already going home then no need to turn off
                        if (currentScenario?.ToString() == _indoorScenario ||
                            currentScenario?.ToString() == _outdoorScenario &&
                            currentOutdoorSubScenario?.ToString() == _outdoorSubScenarioGoingHome)
                            return;

                        await ProcessVirtualTowelHeaterTurningOffAlarmClockEvents(args).ConfigureAwait(false);
                    });
                    break;
            }
        }
    }
}