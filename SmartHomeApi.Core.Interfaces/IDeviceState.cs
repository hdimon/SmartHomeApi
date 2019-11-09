using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceState
    {
        string DeviceId { get; }
        string DeviceType { get; }
        ConnectionStatus ConnectionStatus { get; set; }

        Dictionary<string, object> Telemetry { get; set; }
    }
}