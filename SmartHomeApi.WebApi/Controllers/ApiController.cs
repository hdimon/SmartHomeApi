using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ISmartHomeApiFabric _fabric;

        public ApiController(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        [HttpGet]
        [Route("[action]")]
        public IDeviceStatesContainer GetState()
        {
            var manager = _fabric.GetDeviceManager();

            return manager.GetState();
        }

        [HttpGet]
        [Route("[action]/{deviceId}")]
        public IDeviceState GetState(string deviceId)
        {
            var manager = _fabric.GetDeviceManager();

            return manager.GetState(deviceId);
        }

        [HttpGet]
        [Route("[action]/{deviceId}/{parameter}")]
        public object GetState(string deviceId, string parameter)
        {
            var manager = _fabric.GetDeviceManager();

            return manager.GetState(deviceId, parameter);
        }

        /*[HttpPost]
        public async Task SetValue(string deviceId, string parameter, string value)
        {
            var manager = _fabric.GetDeviceManager();

            await manager.SetValue(deviceId, parameter, value);
        }*/

        [HttpGet]
        [Route("[action]/{deviceId}/{parameter}/{value?}")]
        public async Task SetValue(string deviceId, string parameter, string value)
        {
            var manager = _fabric.GetDeviceManager();

            await manager.SetValue(deviceId, parameter, value);
        }
    }
}