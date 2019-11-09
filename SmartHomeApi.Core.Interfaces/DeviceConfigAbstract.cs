namespace SmartHomeApi.Core.Interfaces
{
    public class DeviceConfigAbstract : IDeviceConfig
    {
        public string DeviceId { get; }
        public string DeviceType { get; }

        public DeviceConfigAbstract(string deviceId, string deviceType)
        {
            DeviceId = deviceId;
            DeviceType = deviceType;
        }
    }
}