﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;

namespace SmartHomeApi.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [CustomJsonFormatter]
    public class ApiController : ControllerBase
    {
        private readonly ISmartHomeApiFabric _fabric;

        public ApiController(ISmartHomeApiFabric fabric)
        {
            _fabric = fabric;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IStatesContainer> GetState()
        {
            var manager = _fabric.GetApiManager();

            return await manager.GetState(true);
        }

        [HttpGet]
        [Route("[action]/{deviceId}")]
        public async Task<IItemState> GetState(string deviceId)
        {
            var manager = _fabric.GetApiManager();

            return await manager.GetState(deviceId, true);
        }

        [HttpGet]
        [Route("[action]/{deviceId}/{parameter}")]
        public async Task<object> GetState(string deviceId, string parameter)
        {
            var manager = _fabric.GetApiManager();

            return await manager.GetState(deviceId, parameter, true);
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

        [HttpGet]
        [Route("{execute}/{itemId}/{command}")]
        public async Task<IActionResult> ExecuteGet(string itemId, string command,
            [FromQuery] Dictionary<string, string> query)
        {
            var executeCommand = new ExecuteCommand();
            executeCommand.ItemId = itemId;
            executeCommand.Command = command;
            executeCommand.HttpMethod = ExecuteCommandHttpMethod.Get;
            executeCommand.QueryParams = query;

            var manager = _fabric.GetApiManager();

            var result = await manager.Execute(executeCommand);

            return GetExecutingResult(result);
        }

        [HttpPost]
        [Route("{execute}/{itemId}/{command}")]
        public async Task<IActionResult> ExecutePost(string itemId, string command,
            [FromQuery] Dictionary<string, string> query, [FromBody] JObject body)
        {
            var bodyDict = body?.ToDictionary();

            var executeCommand = new ExecuteCommand();
            executeCommand.ItemId = itemId;
            executeCommand.Command = command;
            executeCommand.HttpMethod = ExecuteCommandHttpMethod.Post;
            executeCommand.QueryParams = query;
            executeCommand.BodyParams = bodyDict;

            var manager = _fabric.GetApiManager();

            var result = await manager.Execute(executeCommand);

            return GetExecutingResult(result);
        }

        private IActionResult GetExecutingResult(ExecuteCommandResultAbstract result)
        {
            switch (result)
            {
                case ExecuteCommandResultNotFound _:
                    return NotFound(/*result*/);
                case ExecuteCommandResultInternalError _:
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                case ExecuteCommandResultFileContent content:
                {
                    return File(content.FileContents, content.ContentType);
                }
                default:
                    return Ok(result);
            }
        }
    }
}