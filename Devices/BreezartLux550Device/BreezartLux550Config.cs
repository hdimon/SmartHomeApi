using SmartHomeApi.Core.Interfaces;

namespace BreezartLux550Device
{
    public class BreezartLux550Config : DeviceConfigAbstract
    {
        public string IpAddress { get; set; }

        public BreezartLux550Config(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}