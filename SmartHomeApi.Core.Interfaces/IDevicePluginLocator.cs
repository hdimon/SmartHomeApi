using System.Collections.Generic;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDevicePluginLocator
    {
        List<IDeviceLocator> GetDeviceLocators();
    }
}