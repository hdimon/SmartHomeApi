using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public abstract class DeviceAbstract : IItem, IStateSettable, IStateGettable, IConfigurable, IInitializable
    {
        protected readonly IDeviceHelpersFabric HelpersFabric;
        public string ItemId { get; }
        public string ItemType { get; }
        public IDeviceConfig Config { get; }

        protected DeviceAbstract(IDeviceHelpersFabric helpersFabric, IDeviceConfig config)
        {
            HelpersFabric = helpersFabric;
            Config = config;

            ItemId = config.DeviceId;
            ItemType = config.DeviceType;
        }

        public abstract Task<ISetValueResult> SetValue(string parameter, string value);
        public abstract IItemState GetState();

        public bool IsInitialized { get; set; }

        public async Task Initialize()
        {
            if (IsInitialized)
                return;

            await InitializeDevice();
            IsInitialized = true;
        }

        protected virtual async Task InitializeDevice()
        {
        }
    }
}