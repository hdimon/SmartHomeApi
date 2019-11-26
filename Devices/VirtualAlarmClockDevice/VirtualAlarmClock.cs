﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace VirtualAlarmClockDevice
{
    public class VirtualAlarmClock : DeviceAbstract
    {
        private readonly IDeviceStateStorageHelper _deviceStateStorage;
        private ConcurrentDictionary<string, string> _states;
        private Task _worker;
        private DateTime? _time;

        private const string TimeParameter = "Time";
        private const string NextAlarmDateTimeParameter = "NextAlarmDateTime";
        private const string AlarmParameter = "Alarm";
        private const string EnabledParameter = "Enabled";

        public VirtualAlarmClock(IDeviceHelpersFabric helpersFabric, IDeviceConfig config) : base(helpersFabric, config)
        {
            _deviceStateStorage = HelpersFabric.GetDeviceStateStorageHelper();
        }

        protected override async Task InitializeDevice()
        {
            _states = _deviceStateStorage.RestoreState<ConcurrentDictionary<string, string>>(ItemId);

            if (_states == null)
            {
                _states = new ConcurrentDictionary<string, string>();
                _states.TryAdd(EnabledParameter, "true");
            }

            if (_states.ContainsKey(TimeParameter))
            {
                Task.Run(async () => await SetTime(TimeParameter, _states[TimeParameter])).ContinueWith(t =>
                {
                    Logger.Error(t.Exception);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            RunWatchDogWorker();
        }

        public override IItemState GetState()
        {
            var state = new ItemState(ItemId, ItemType);
            state.ConnectionStatus = ConnectionStatus.Stable;

            foreach (var statePair in _states)
            {
                object value = statePair.Value;

                if (statePair.Key == EnabledParameter)
                    value = bool.Parse(statePair.Value);

                state.States.TryAdd(statePair.Key, value);
            }

            return state;
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
        {
            switch (parameter)
            {
                case TimeParameter:
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _states.TryRemove(parameter, out _);
                        _time = null;
                    }

                    await SetTime(parameter, value);
                    break;
                case EnabledParameter:
                    SetEnabledParameter(value);
                    break;
                case AlarmParameter:
                    await ResetAlarmParameter(value);
                    break;
            }

            await _deviceStateStorage.SaveState(_states, ItemId);

            return new SetValueResult();
        }

        private async Task SetTime(string parameter, string value)
        {
            if (!DateTime.TryParse(value, out var t))
                return;

            t = GetNextAlarmDateTime(t);

            var nextAlarmDateTime = t.ToString();
            _states.AddOrUpdate(parameter, value, (key, oldValue) => value);
            _states.AddOrUpdate(NextAlarmDateTimeParameter, nextAlarmDateTime, (key, oldValue) => nextAlarmDateTime);
            await ResetAlarmParameter(null);
            _time = t;
        }

        private DateTime GetNextAlarmDateTime(DateTime time)
        {
            var now = DateTime.Now;

            if (time < DateTime.Now && time.Date == now.Date)
                time = time.AddDays(1);

            return time;
        }

        private void SetEnabledParameter(string value)
        {
            if (value == "1")
                value = "true";
            else if (value == "0")
            {
                value = "false";
            }

            if (!bool.TryParse(value, out var enabled))
                return;

            if (!enabled)
            {
                _time = null;
            }
            else
            {
                if (!_states.ContainsKey(TimeParameter))
                    return;

                var timeStr = _states[TimeParameter];
                SetTime(TimeParameter, timeStr);
            }

            _states.AddOrUpdate(EnabledParameter, value, (s, s1) => value);
        }

        private async Task ResetAlarmParameter(string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) 
                return;

            var config = (VirtualAlarmClockConfig)Config;

            for (int i = 0; i < 10; i++)
            {
                _states.TryRemove(AlarmParameter, out _);

                if (_states.ContainsKey(AlarmParameter))
                    await Task.Delay(100);
                else
                {
                    if (!config.EveryDay)
                        _time = null;
                    break;
                }
            }
        }

        private void RunWatchDogWorker()
        {
            if (_worker == null || _worker.IsCompleted)
            {
                _worker = Task.Factory.StartNew(CheckAlarm).Unwrap().ContinueWith(
                    t =>
                    {
                        Logger.Error(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task CheckAlarm()
        {
            while (true)
            {
                await Task.Delay(1000);

                if (!_time.HasValue)
                    continue;

                if (_time.Value < DateTime.Now)
                {
                    _states.AddOrUpdate(AlarmParameter, _time.ToString(), (key, oldValue) => _time.ToString());

                    var config = (VirtualAlarmClockConfig)Config;
                    if (config.EveryDay)
                    {
                        SetNextTime();
                    }

                    await _deviceStateStorage.SaveState(_states, ItemId);
                }
            }
        }

        private void SetNextTime()
        {
            if (!_time.HasValue)
                return;

            var nextAlarmTime = _time.Value.AddDays(1);
            _time = nextAlarmTime;

            var nextAlarmTimeStr = nextAlarmTime.ToString();

            _states.AddOrUpdate(NextAlarmDateTimeParameter, nextAlarmTimeStr, (key, oldValue) => nextAlarmTimeStr);
        }
    }
}