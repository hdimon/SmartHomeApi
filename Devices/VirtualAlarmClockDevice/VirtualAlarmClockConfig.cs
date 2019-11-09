using SmartHomeApi.Core.Interfaces;

namespace VirtualAlarmClockDevice
{
    public class VirtualAlarmClockConfig : DeviceConfigAbstract
    {
        public bool EveryDay { get; set; }

        public VirtualAlarmClockConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}