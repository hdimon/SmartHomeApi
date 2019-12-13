using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartHomeApi.Core.Interfaces;

namespace Scenarios
{
    class VentilationSpeedManager
    {
        private readonly TimeSpan _measurementPeriod;
        private readonly TimeSpan _halfMeasurementPeriod;
        private int _minSpeed = 1;
        private int _firstLevelDefaultSpeed = 4;
        private int _secondLevelDefaultSpeed = 7;
        private int _maxSpeed = 10;
        private int _recommendedSpeed;

        private readonly ConcurrentDictionary<string, List<StateChangedEvent>> _devicesTelemetry =
            new ConcurrentDictionary<string, List<StateChangedEvent>>();

        public VentilationSpeedManager(TimeSpan measurementPeriod)
        {
            _recommendedSpeed = _minSpeed;
            _measurementPeriod = measurementPeriod;
            _halfMeasurementPeriod = _measurementPeriod / 2;
        }

        public void AddEvent(StateChangedEvent args)
        {
            if (!_devicesTelemetry.ContainsKey(args.DeviceId))
                _devicesTelemetry.AddOrUpdate(args.DeviceId, new List<StateChangedEvent>(),
                    (s, list) => new List<StateChangedEvent>());

            var events = _devicesTelemetry[args.DeviceId];

            events.Add(args);

            _devicesTelemetry[args.DeviceId] = GetFilteredData(events);
        }

        private List<StateChangedEvent> GetFilteredData(List<StateChangedEvent> events)
        {
            var now = DateTimeOffset.Now;
            var startOfPeriod = now - _measurementPeriod;

            if (events.Any(e => e.EventDate < startOfPeriod))
                events = events.Where(e => e.EventDate >= startOfPeriod).ToList();

            return events;
        }

        public int GetRecommendedSpeed()
        {
            var now = DateTimeOffset.Now;
            var middleOfMeasurementPeriod = now - _halfMeasurementPeriod;

            var recommendedSpeeds = new Dictionary<string, int>();

            foreach (var pair in _devicesTelemetry)
            {
                var values = pair.Value.OrderBy(e => e.EventDate).ToList();

                if (!values.Any())
                {
                    recommendedSpeeds.Add(pair.Key, _minSpeed);
                    continue;
                }

                var beginValues = new List<int>();
                var endValues = new List<int>();

                if (values.Count == 1)
                {
                    var ev = values.First();

                    if (string.IsNullOrWhiteSpace(ev.OldValue))
                    {
                        int speed = GetInitialRecommendedSpeed(ev.NewValue);

                        recommendedSpeeds.Add(pair.Key, speed);
                        continue;
                    }

                    var res1 = int.TryParse(ev.OldValue, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out var beginCo2ppm);
                    var res2 = int.TryParse(ev.NewValue, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out var endCo2ppm);

                    if (!res1 || !res2)
                    {
                        recommendedSpeeds.Add(pair.Key, _minSpeed);
                        continue;
                    }

                    beginValues.Add(beginCo2ppm);
                    endValues.Add(endCo2ppm);
                }
                else
                {
                    if (int.TryParse(values.First().OldValue, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out var co2ppm))
                        beginValues.Add(co2ppm);

                    var beginEvents = values.Where(e => e.EventDate < middleOfMeasurementPeriod).ToList();
                    var endEvents = values.Where(e => e.EventDate >= middleOfMeasurementPeriod).ToList();

                    if (!endEvents.Any())
                    {
                        //Take last event from beginning half of events
                        var last = beginEvents.Last();
                        endEvents.Add(last);
                        beginEvents.Remove(last);
                    }

                    foreach (var @event in beginEvents)
                    {
                        if (int.TryParse(@event.NewValue, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out co2ppm))
                            beginValues.Add(co2ppm);
                    }

                    foreach (var @event in endEvents)
                    {
                        if (int.TryParse(@event.NewValue, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out co2ppm))
                            endValues.Add(co2ppm);
                    }
                }

                if (!beginValues.Any())
                {
                    var average = (int)Math.Round(endValues.Average(), 0);

                    int speed = GetInitialRecommendedSpeed(average);

                    recommendedSpeeds.Add(pair.Key, speed);
                    continue;
                }

                var beginAverage = (int)Math.Round(beginValues.Average(), 0);
                var endAverage = (int)Math.Round(endValues.Average(), 0);

                var changeDuringPeriod = endAverage - beginAverage;
                var rec = GetRecommendedSpeed(changeDuringPeriod, endAverage);
                recommendedSpeeds.Add(pair.Key, rec);
            }

            int recommendedSpeed = recommendedSpeeds.Any() ? recommendedSpeeds.Max(p => p.Value) : _minSpeed;

            _recommendedSpeed = recommendedSpeed;
            return _recommendedSpeed;
        }

        private int GetInitialRecommendedSpeed(string co2ppmStr)
        {
            if (string.IsNullOrWhiteSpace(co2ppmStr) || !int.TryParse(co2ppmStr, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var co2ppm))
            {
                return _minSpeed;
            }

            return GetInitialRecommendedSpeed(co2ppm);
        }

        private int GetInitialRecommendedSpeed(int co2ppm)
        {
            if (co2ppm <= 600)
            {
                return _minSpeed;
            }

            if (co2ppm > 600 && co2ppm <= 800)
            {
                return _firstLevelDefaultSpeed;
            }

            if (co2ppm > 800 && co2ppm <= 1000)
            {
                return _secondLevelDefaultSpeed;
            }

            return _maxSpeed;
        }

        private int GetRecommendedSpeed(int changeDuringPeriod, int endAverage)
        {
            var recommendedSpeed = _recommendedSpeed;

            //Concentration is decreasing
            if (changeDuringPeriod < 0)
            {
                var change = Math.Abs(changeDuringPeriod);

                if (change <= 10)
                {
                    //Accelerate decreasing
                    if (endAverage > 1200)
                        recommendedSpeed = _recommendedSpeed + 3;
                    else if (endAverage > 1000)
                        recommendedSpeed = _recommendedSpeed + 2;
                    else if (endAverage > 800)
                        recommendedSpeed = _recommendedSpeed + 1;
                }
                else
                {
                    //Air is clear enough so can decrease speed
                    if (endAverage < 800)
                        recommendedSpeed = _recommendedSpeed - 1;
                    //Otherwise leave speed as is
                }
            }
            else if (changeDuringPeriod == 0)
            {
                //Initiate decreasing
                if (endAverage > 1200)
                    recommendedSpeed = _recommendedSpeed + 3;
                else if (endAverage > 1000)
                    recommendedSpeed = _recommendedSpeed + 2;
                else if (endAverage > 800)
                    recommendedSpeed = _recommendedSpeed + 1;
                //Otherwise leave speed as is
            }
            else //Concentration is increasing
            {
                //Try to decrease
                if (endAverage > 1200)
                    recommendedSpeed = _recommendedSpeed + 4;
                else if (endAverage > 1100) //1000
                    recommendedSpeed = _recommendedSpeed + 3;
                else if (endAverage > 900) //800
                    recommendedSpeed = _recommendedSpeed + 2;
                else if (endAverage > 700) //600
                    recommendedSpeed = _recommendedSpeed + 1;

                /*if (changeDuringPeriod <= 10)
                {
                    //Try to decrease
                    if (endAverage > 800) 
                        recommendedSpeed = _recommendedSpeed + 1;
                }
                else
                {
                    if (endAverage > 600) //Try to decrease
                        recommendedSpeed = _recommendedSpeed + 1;
                }*/
            }

            if (recommendedSpeed < _minSpeed)
                recommendedSpeed = _minSpeed;

            if (recommendedSpeed > _maxSpeed)
                recommendedSpeed = _maxSpeed;

            return recommendedSpeed;
        }
    }
}