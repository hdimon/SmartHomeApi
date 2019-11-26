using SmartHomeApi.Core.Interfaces;

namespace Mega2560ControllerDevice
{
    public class Mega2560ControllerConfig : DeviceConfigAbstract
    {
        public string Mac { get; set; }
        public string IpAddress { get; set; }

        public Mega2560ControllerConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}