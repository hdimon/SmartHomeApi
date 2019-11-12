using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IDevice
    {
        string DeviceId { get; }
        string DeviceType { get; }
        IDeviceConfig Config { get; }

        IDeviceState GetState();

        Task<ISetValueResult> SetValue(string parameter, string value);
    }
}