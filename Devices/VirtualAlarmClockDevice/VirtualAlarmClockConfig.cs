using SmartHomeApi.Core.Interfaces;

namespace VirtualAlarmClockDevice
{
    public class VirtualAlarmClockConfig : ItemConfigAbstract
    {
        public bool EveryDay { get; set; }

        public VirtualAlarmClockConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}