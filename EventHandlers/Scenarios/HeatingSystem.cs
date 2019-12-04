using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class HeatingSystem : StateChangedSubscriberAbstract
    {
        private readonly int _heatingSystemMorningAdvanceMinutes = 45;
        private readonly int _heatingSystemMorningDurationMinutes = 60;
        private readonly int _failoverActionIntervalSeconds = 30;

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
                    results = await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Bedroom_Floor", "SetTemperature", "20"))
                                        .ConfigureAwait(false);
                    break;
                case "Indoor":
                    results = await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "29"),
                                            Manager.SetValue("Bathroom_Floor", "SetTemperature", "28"),
                                            Manager.SetValue("Toilet_Floor", "SetTemperature", "28"),
                                            Manager.SetValue("Bedroom_Floor", "SetTemperature", "26"))
                                        .ConfigureAwait(false);
                    break;
                case "Sleep":
                    results = await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                                            Manager.SetValue("Bedroom_Floor", "SetTemperature", "24"))
                                        .ConfigureAwait(false);
                    break;
                case "Morning":
                    results = await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "30"),
                                            Manager.SetValue("Bathroom_Floor", "SetTemperature", "30"),
                                            Manager.SetValue("Toilet_Floor", "SetTemperature", "30"),
                                            Manager.SetValue("Bedroom_Floor", "SetTemperature", "26"))
                                        .ConfigureAwait(false);
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
                    if (!bool.TryParse(args.NewValue, out var enabled))
                        return;

                    await Manager.SetValue("Virtual_HeatingSystemMorningAlarmClock", "Enabled", args.NewValue)
                                 .ConfigureAwait(false);
                    await Manager.SetValue("Virtual_HeatingSystemAfterMorningAlarmClock", "Enabled", args.NewValue)
                                 .ConfigureAwait(false);
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

                    var currentScenario = Manager.GetState("Virtual_States", "Scenario");

                    //1. If scenario is already morning it means alarm clock was reset
                    //(for example initially it was set on 8:30 but in 8:20 or 8:40 (i.e. during Morning interval) it was reset to 8:50)
                    //then just cancel alarm.
                    //2. If scenario is Outdoor it means nobody is in home so just cancel alarm, no need to heat flat.
                    //3. If scenario is Indoor it means daytime sleep, cancel alarm because flat is already heated likely.
                    if (currentScenario?.ToString() == "Morning" || currentScenario?.ToString() == "Outdoor" ||
                        currentScenario?.ToString() == "Indoor")
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

                    await Manager.SetValue("Virtual_States", "Scenario", "Morning").ConfigureAwait(false);
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
                    await Manager.SetValue("Virtual_States", "Scenario", "Indoor").ConfigureAwait(false);
                    await Manager.SetValue("Virtual_HeatingSystemAfterMorningAlarmClock", "Alarm", null)
                                 .ConfigureAwait(false);
                    break;
            }
        }
    }
}