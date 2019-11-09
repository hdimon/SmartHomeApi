using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDeviceConfigLocator
    {
        List<IDeviceConfig> GetDeviceConfigs(string deviceType);
    }
}