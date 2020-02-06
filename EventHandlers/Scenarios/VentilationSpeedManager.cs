using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    class VentilationSpeedManager
    {
        private readonly TimeSpan _measurementPeriod;
        private int _minSpeed = 1;
        private int _recommendedSpeed;

        private readonly ConcurrentDictionary<string, SensorVentilationSpeedManager> _sensorsManagers =
            new ConcurrentDictionary<string, SensorVentilationSpeedManager>();

        public VentilationSpeedManager(TimeSpan measurementPeriod)
        {
            _recommendedSpeed = _minSpeed;
            _measurementPeriod = measurementPeriod;
        }

        public void AddEvent(StateChangedEvent args)
        {
            if (!_sensorsManagers.ContainsKey(args.DeviceId))
                _sensorsManagers.AddOrUpdate(args.DeviceId, new SensorVentilationSpeedManager(_measurementPeriod),
                    (s, list) => new SensorVentilationSpeedManager(_measurementPeriod));

            _sensorsManagers[args.DeviceId].AddEvent(args);
        }

        public int GetRecommendedSpeed()
        {
            var recommendedSpeeds = new List<int>();

            foreach (var pair in _sensorsManagers)
            {
                recommendedSpeeds.Add(pair.Value.GetRecommendedSpeed());
            }

            int recommendedSpeed = recommendedSpeeds.Any() ? recommendedSpeeds.Max(p => p) : _minSpeed;

            _recommendedSpeed = recommendedSpeed;
            return _recommendedSpeed;
        }
    }
}