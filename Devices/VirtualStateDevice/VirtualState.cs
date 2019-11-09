using System.Collections.Concurrent;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace VirtualStateDevice
{
    public class VirtualState : DeviceAbstract
    {
        private IDeviceState _state;
        private readonly ConcurrentDictionary<string, string> _states;
        private readonly IDeviceStateStorageHelper _deviceStateStorage;

        public VirtualState(IDeviceHelpersFabric helpersFabric, IDeviceConfig config) : base(helpersFabric, config)
        {
            _deviceStateStorage = HelpersFabric.GetDeviceStateStorageHelper();

            _states = _deviceStateStorage.RestoreState<ConcurrentDictionary<string, string>>(DeviceId);

            if (_states == null)
                _states = new ConcurrentDictionary<string, string>();
        }

        public override IDeviceState GetState()
        {
            var state = new DeviceState(DeviceId, DeviceType);
            state.ConnectionStatus = ConnectionStatus.Stable;

            foreach (var statePair in _states)
            {
                state.Telemetry.TryAdd(statePair.Key, statePair.Value);
            }

            _state = state;

            return _state;
        }

        public override async Task SetValue(string parameter, string value)
        {
            if (_states.ContainsKey(parameter))
            {
                if (string.IsNullOrWhiteSpace(value))
                    _states.TryRemove(parameter, out _);
                else
                    _states[parameter] = value;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _states.TryAdd(parameter, value);
            }

            await _deviceStateStorage.SaveState(_states, DeviceId);
        }
    }
}