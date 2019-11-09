using System.Collections.Generic;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Models
{
    public class DeviceStatesContainer : IDeviceStatesContainer
    {
        public Dictionary<string, IDeviceState> DevicesStates { get; set; } = new Dictionary<string, IDeviceState>();
    }
}