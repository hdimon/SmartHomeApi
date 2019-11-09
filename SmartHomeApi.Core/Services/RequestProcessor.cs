using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class RequestProcessor : IRequestProcessor
    {
        public async Task SetValue(string deviceId, string parameter, string value)
        {
        }

        public async Task Increase(string deviceId, string parameter)
        {
        }

        public async Task Decrease(string deviceId, string parameter)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IDeviceState> GetState()
        {
            throw new System.NotImplementedException();
        }

        public async Task<IDeviceState> GetState(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<object> GetState(string deviceId, string parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
