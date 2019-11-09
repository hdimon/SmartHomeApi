using System.Threading.Tasks;

namespace SmartHomeApi.Core.Interfaces
{
    public interface IRequestProcessor
    {
        Task SetValue(string deviceId, string parameter, string value);
        Task Increase(string deviceId, string parameter);
        Task Decrease(string deviceId, string parameter);
        Task<IDeviceState> GetState();
        Task<IDeviceState> GetState(string deviceId);
        Task<object> GetState(string deviceId, string parameter);
    }
}