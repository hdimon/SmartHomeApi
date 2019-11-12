using System;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    public class HeatingSystem : EventHandlerAbstract
    {
        private readonly int _heatingSystemMorningAdvanceMinutes = 45;
        private readonly int _heatingSystemMorningDurationMinutes = 60;

        public HeatingSystem(IDeviceManager manager) : base(manager)
        {
        }

        protected override async Task ProcessNotification(StateChangedEvent args)
        {
            if (args == null)
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
            switch (args.NewValue)
            {
                case "Outdoor":
                    await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Bedroom_Floor", "SetTemperature", "20")).ConfigureAwait(false);
                    break;
                case "Indoor":
                    await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "29"),
                        Manager.SetValue("Bathroom_Floor", "SetTemperature", "28"),
                        Manager.SetValue("Toilet_Floor", "SetTemperature", "28"),
                        Manager.SetValue("Bedroom_Floor", "SetTemperature", "26")).ConfigureAwait(false);
                    break;
                case "Sleep":
                    await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Bathroom_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Toilet_Floor", "SetTemperature", "20"),
                        Manager.SetValue("Bedroom_Floor", "SetTemperature", "24")).ConfigureAwait(false);
                    break;
                case "Morning":
                    await Task.WhenAll(Manager.SetValue("Kitchen_Floor", "SetTemperature", "30"),
                        Manager.SetValue("Bathroom_Floor", "SetTemperature", "30"),
                        Manager.SetValue("Toilet_Floor", "SetTemperature", "30"),
                        Manager.SetValue("Bedroom_Floor", "SetTemperature", "26")).ConfigureAwait(false);
                    break;
            }
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
            }
        }

        private async Task ProcessVirtualHeatingSystemMorningAlarmClockEvents(StateChangedEvent args)
        {
            switch (args.Parameter)
            {
                case "Alarm":
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