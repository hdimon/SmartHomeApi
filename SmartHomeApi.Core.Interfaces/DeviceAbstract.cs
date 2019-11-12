using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public abstract class DeviceAbstract : IDevice
    {
        protected readonly IDeviceHelpersFabric HelpersFabric;
        public string DeviceId { get; }
        public string DeviceType { get; }
        public IDeviceConfig Config { get; }

        protected DeviceAbstract(IDeviceHelpersFabric helpersFabric, IDeviceConfig config)
        {
            HelpersFabric = helpersFabric;
            Config = config;
            DeviceId = config.DeviceId;
            DeviceType = config.DeviceType;
        }

        public abstract IDeviceState GetState();

        public abstract Task<ISetValueResult> SetValue(string parameter, string value);
    }
}