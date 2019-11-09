using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceLocator
    {
        string DeviceType { get; }
        List<IDevice> GetDevices();
    }
}