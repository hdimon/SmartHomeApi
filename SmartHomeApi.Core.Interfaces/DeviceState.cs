using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public class DeviceState : IDeviceState
    {
        public string DeviceId { get; }
        public string DeviceType { get; }

        public ConnectionStatus ConnectionStatus { get; set; }

        public Dictionary<string, object> Telemetry { get; set; } = new Dictionary<string, object>();

        public DeviceState(string deviceId, string deviceType)
        {
            DeviceId = deviceId;
            DeviceType = deviceType;
            ConnectionStatus = ConnectionStatus.Unknown;
        }
    }
}