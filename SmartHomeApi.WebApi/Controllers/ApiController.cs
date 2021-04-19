using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ExecuteCommandResults;
using SmartHomeApi.Core.Interfaces.Extensions;

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

            return await manager.GetState();
        }

        [HttpGet]
        [Route("[action]/{itemId}")]
        public async Task<IItemState> GetState(string itemId)
        {
            var manager = _fabric.GetApiManager();

            return await manager.GetState(itemId);
        }

        [HttpGet]
        [Route("[action]/{itemId}/{parameter}/{locale?}")]
        public async Task<object> GetState(string itemId, string parameter, string locale)
        {
            var manager = _fabric.GetApiManager();

            CultureInfo culture = GetCultureInfo(locale);

            if (culture == null)
                throw new ArgumentException($"Culture [{locale}] is not valid.");

            var obj = await manager.GetState(itemId, parameter);

            if (obj is bool)
                return obj.ToString()?.ToLower();

            return Convert.ToString(obj, culture);
        }

        [HttpPost]
        [Route("[action]/{itemId}/{parameter}/{value?}/{type?}/{locale?}")]
        public async Task<ISetValueResult> SetValue(string itemId, string parameter, string value, string type, string locale)
        {
            var manager = _fabric.GetApiManager();
            var logger = _fabric.GetApiLogger();

            if (value == null)
            {
                var result = await manager.SetValue(itemId, parameter, null);

                return result;
            }

            if (!Enum.TryParse<ValueDataType>(type, true, out var valueType))
            {
                var validTypes = Enum.GetNames(typeof(ValueDataType));
                logger.Error($"DataType [{type}] is not valid. Valid types are: {string.Join(", ", validTypes)}.");
                return new SetValueResult(false);
            }

            CultureInfo culture = GetCultureInfo(locale);

            if (culture == null)
                return new SetValueResult(false);

            object objValue;

            try
            {
                objValue = value.GetAsObject(valueType, culture);
            }
            catch (Exception)
            {
                logger.Error($"Can't cast [{value}] of type [{type}] to Object.");

                return new SetValueResult(false);
            }

            return await manager.SetValue(itemId, parameter, objValue);
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
                {
                    if (_fabric.GetConfiguration().ItemsPluginsLocator.SoftPluginsLoading)
                    {
                        //Since plugin type is cached in Newtonsoft and it can't be fully disabled (see explanation in ApiDefaultContractResolver)
                        //then convert type to dynamic object. It allows to unload plugin.
                        var dyn = TypeHelper.ObjToDynamic(result);
                        return Ok(dyn);
                    }

                    return Ok(result);
                }
            }
        }

        private CultureInfo GetCultureInfo(string locale)
        {
            var logger = _fabric.GetApiLogger();
            CultureInfo culture = null;

            if (!string.IsNullOrWhiteSpace(locale))
            {
                try
                {
                    if (!CultureInfoHelper.Exists(locale))
                    {
                        logger.Error($"Culture [{locale}] is not valid.");
                        return null;
                    }

                    culture = CultureInfo.GetCultureInfo(locale);
                }
                catch (Exception)
                {
                    logger.Error($"Culture [{locale}] is not valid.");
                    return null;
                }
            }

            if (culture == null)
                culture = Thread.CurrentThread.CurrentCulture;

            return culture;
        }
    }
}