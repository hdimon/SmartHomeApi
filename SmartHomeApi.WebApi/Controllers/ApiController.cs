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
        public IStatesContainer GetState()
        {
            var manager = _fabric.GetApiManager();

            return manager.GetState();
        }

        [HttpGet]
        [Route("[action]/{deviceId}")]
        public IItemState GetState(string deviceId)
        {
            var manager = _fabric.GetApiManager();

            return manager.GetState(deviceId);
        }

        [HttpGet]
        [Route("[action]/{deviceId}/{parameter}")]
        public object GetState(string deviceId, string parameter)
        {
            var manager = _fabric.GetApiManager();

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
        public async Task<ISetValueResult> SetValue(string deviceId, string parameter, string value)
        {
            var manager = _fabric.GetApiManager();

            var result = await manager.SetValue(deviceId, parameter, value);

            return result;
        }
    }
}