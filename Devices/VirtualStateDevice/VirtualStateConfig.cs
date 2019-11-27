using SmartHomeApi.Core.Interfaces;

namespace VirtualStateDevice
{
    public class VirtualStateConfig : ItemConfigAbstract
    {
        public VirtualStateConfig(string deviceId, string deviceType) : base(deviceId, deviceType)
        {
        }
    }
}