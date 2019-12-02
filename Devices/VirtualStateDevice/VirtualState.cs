using System.Collections.Concurrent;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace VirtualStateDevice
{
    public class VirtualState : DeviceAbstract
    {
        private IItemState _state;
        private ConcurrentDictionary<string, string> _states;
        private readonly IItemStateStorageHelper _deviceStateStorage;

        public VirtualState(IItemHelpersFabric helpersFabric, IItemConfig config) : base(helpersFabric, config)
        {
            _deviceStateStorage = HelpersFabric.GetDeviceStateStorageHelper();
        }

        protected override async Task InitializeDevice()
        {
            _states = _deviceStateStorage.RestoreState<ConcurrentDictionary<string, string>>(ItemId);

            if (_states == null)
                _states = new ConcurrentDictionary<string, string>();
        }

        public override IItemState GetState()
        {
            var state = new ItemState(ItemId, ItemType);
            state.ConnectionStatus = ConnectionStatus.Stable;

            foreach (var statePair in _states)
            {
                state.States.TryAdd(statePair.Key, statePair.Value);
            }

            _state = state;

            return _state;
        }

        public override async Task<ISetValueResult> SetValue(string parameter, string value)
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

            await _deviceStateStorage.SaveState(_states, ItemId);

            return new SetValueResult();
        }
    }
}