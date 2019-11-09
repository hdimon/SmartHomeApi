using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.UnitTests.Stubs;
using VirtualAlarmClockDevice;

namespace SmartHomeApi.Core.UnitTests
{
    public class VirtualAlarmClockTests
    {
        [Test]
        public async Task SetEveryDayAlarmOnTomorrowTest1()
        {
            var fabric = new DeviceHelpersStubFabric();
            var config = new VirtualAlarmClockConfig("Virtual_MainAlarmClock", "VirtualAlarmClockDevice")
                { EveryDay = true };

            var alarm = new VirtualAlarmClock(fabric, config);

            var state = alarm.GetState();
            Assert.AreEqual(1, state.Telemetry.Count);

            var now = DateTime.Now;

            var time = now.AddSeconds(-1);
            var timeStr = time.ToLongTimeString();
            var nextAlarmStr = time.AddDays(1).ToString();

            await alarm.SetValue("Time", timeStr);

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);
        }

        [Test]
        public async Task SetEveryDayAlarmOnTodayTest1()
        {
            var fabric = new DeviceHelpersStubFabric();
            var config = new VirtualAlarmClockConfig("Virtual_MainAlarmClock", "VirtualAlarmClockDevice")
                { EveryDay = true };

            var alarm = new VirtualAlarmClock(fabric, config);

            var state = alarm.GetState();
            Assert.AreEqual(1, state.Telemetry.Count);

            var now = DateTime.Now;

            var time = now.AddSeconds(2);
            var timeStr = time.ToLongTimeString();
            var nextAlarmStr = time.ToString();

            await alarm.SetValue("Time", timeStr);

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);

            await Task.Delay(3000);

            state = alarm.GetState();

            Assert.AreEqual(4, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["Alarm"]);

            var nextAlarm = time.AddDays(1);
            nextAlarmStr = nextAlarm.ToString();

            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);

            //Set another time. Alarm parameter must be reset.
            await alarm.SetValue("Time", DateTime.Now.AddHours(1).ToLongTimeString());

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.IsFalse(state.Telemetry.ContainsKey("Alarm"));
        }

        [Test]
        public async Task SetEveryDayAlarmOnTodayAndDisableTest1()
        {
            var fabric = new DeviceHelpersStubFabric();
            var config = new VirtualAlarmClockConfig("Virtual_MainAlarmClock", "VirtualAlarmClockDevice")
                { EveryDay = true };

            var alarm = new VirtualAlarmClock(fabric, config);

            var state = alarm.GetState();
            Assert.AreEqual(1, state.Telemetry.Count);

            var now = DateTime.Now;

            var time = now.AddSeconds(2);
            var timeStr = time.ToLongTimeString();
            var nextAlarmStr = time.ToString();

            await alarm.SetValue("Time", timeStr);

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);
            Assert.AreEqual(true, state.Telemetry["Enabled"]);

            await alarm.SetValue("Enabled", "false");

            await Task.Delay(3000);

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);
            Assert.AreEqual(false, state.Telemetry["Enabled"]);

            var nextAlarm = time.AddDays(1);
            nextAlarmStr = nextAlarm.ToString();

            await alarm.SetValue("Enabled", "1");

            state = alarm.GetState();

            Assert.AreEqual(3, state.Telemetry.Count);
            Assert.AreEqual(timeStr, state.Telemetry["Time"]);
            Assert.AreEqual(nextAlarmStr, state.Telemetry["NextAlarmDateTime"]);
            Assert.AreEqual(true, state.Telemetry["Enabled"]);
        }
    }
}
