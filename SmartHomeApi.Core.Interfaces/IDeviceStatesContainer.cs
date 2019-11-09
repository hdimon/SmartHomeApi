using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceStatesContainer
    {
        Dictionary<string, IDeviceState> DevicesStates { get; set; }
    }
}