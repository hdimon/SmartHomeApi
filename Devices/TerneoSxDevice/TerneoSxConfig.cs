using SmartHomeApi.Core.Interfaces;

namespace TerneoSxDevice
{
    public class TerneoSxConfig : DeviceConfigAbstract
    {
        public string SerialNumber { get; set; }
        public string IpAddress { get; set; }
        public int Power { get; set; }

        public TerneoSxConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}