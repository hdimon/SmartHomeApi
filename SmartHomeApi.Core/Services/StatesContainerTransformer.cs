using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class StatesContainerTransformer : IStatesContainerTransformer
    {
        private readonly ISmartHomeApiFabric _fabric;
        private readonly IApiLogger _logger;

        private readonly ConcurrentDictionary<string, StateChangedEvent> _events =
            new ConcurrentDictionary<string, StateChangedEvent>();

        public StatesContainerTransformer(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
            _logger = _fabric.GetApiLogger();
        }

        public void AddStateChangedEvent(StateChangedEvent ev)
        {
            var key = GetEventKey(ev);

            _events.AddOrUpdate(key, s => ev, (s, @event) => ev);
        }

        public void Transform(IStatesContainer state, List<IStateTransformable> transformables)
        {
            //RemoveEventsByTimeout();

            var parameterGroups = _events.GroupBy(e => new { DeviceId = e.Value.ItemId, e.Value.Parameter });
            var parameters = parameterGroups.Select(g => g.OrderBy(e => e.Value.EventDate).First().Value).ToList();

            foreach (var ev in parameters)
            {
                if (state.States.ContainsKey(ev.ItemId) && state.States[ev.ItemId].States.ContainsKey(ev.Parameter))
                {
                    var transformer = transformables.FirstOrDefault(t => t.ItemId == ev.ItemId);

                    var parameterValueObject = state.States[ev.ItemId].States[ev.Parameter];

                    object newValue;
                    TransformationResult result = null;

                    if (transformer != null)
                    {
                        try
                        {
                            result = transformer.Transform(ev.Parameter, ev.OldValue, ev.NewValue);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e);
                        }
                    }

                    if (result == null)
                        newValue = GetConvertedNewValue(parameterValueObject, ev.NewValue);
                    else
                    {
                        if (result.Status == TransformationStatus.Continue)
                            newValue = GetConvertedNewValue(parameterValueObject, ev.NewValue);
                        else if (result.Status == TransformationStatus.Success)
                            newValue = result.TransformedValue;
                        else //If canceled then take original value
                            newValue = parameterValueObject; 
                    }

                    state.States[ev.ItemId].States[ev.Parameter] = newValue;
                }
            }
        }

        private void RemoveEventsByTimeout()
        {
            var timoutTime = DateTimeOffset.Now.AddSeconds(-5);
            var timedOut = _events.Where(e => e.Value.EventDate < timoutTime).ToList();

            foreach (var keyValuePair in timedOut)
            {
                _events.TryRemove(keyValuePair.Key, out _);
            }
        }

        private object GetConvertedNewValue(object parameterValueObject, string value)
        {
            try
            {
                var sourceType = parameterValueObject.GetType().Name;

                switch (sourceType)
                {
                    case "Boolean":
                        if (bool.TryParse(value, out bool boolRes))
                            return boolRes;

                        _logger.Warning(
                            $@"StatesContainerTransformer: Value [{value}] could not be converted to {sourceType} type.");
                        break;
                    case "String":
                        return value;
                    case "Double":
                        if (double.TryParse(value, out double doubleRes))
                            return doubleRes;

                        _logger.Warning(
                            $@"StatesContainerTransformer: Value [{value}] could not be converted to {sourceType} type.");
                        break;
                    case "Int32":
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dRes))
                            return Convert.ToInt32(dRes);

                        _logger.Warning(
                            $@"StatesContainerTransformer: Value [{value}] could not be converted to {sourceType} type.");
                        break;
                    default:
                        _logger.Warning($@"StatesContainerTransformer: No converting is found for {sourceType} type.");
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return parameterValueObject;
        }

        public void RemoveStateChangedEvent(StateChangedEvent ev)
        {
            var key = GetEventKey(ev);
            _events.TryRemove(key, out _);
        }

        public bool ParameterIsTransformed(string deviceId, string parameter)
        {
            return false;
        }

        public bool TransformationIsNeeded()
        {
            return _events.Any();
        }

        private string GetEventKey(StateChangedEvent ev)
        {
            return $"{ev.ItemId}_{ev.Parameter}_{ev.EventDate.UtcTicks}";
        }
    }
}