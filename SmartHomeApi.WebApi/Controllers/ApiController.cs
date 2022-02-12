using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        public async Task<IItemStateModel> GetState(string itemId)
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
                var errorMessage = $"DataType [{type}] is not valid. Valid types are: {string.Join(", ", validTypes)}.";

                logger.Error(errorMessage);

                var result = new SetValueResult(false);
                result.Errors.Add(errorMessage);

                return result;
            }

            CultureInfo culture = GetCultureInfo(locale);

            if (culture == null)
            {
                var result = new SetValueResult(false);
                result.Errors.Add($"Culture [{locale}] is not valid.");

                return result;
            }

            object objValue;

            try
            {
                objValue = value.GetAsObject(valueType, culture);
            }
            catch (Exception)
            {
                var errorMessage = $"Can't cast [{value}] to [{type}] type.";

                logger.Error(errorMessage);

                var result = new SetValueResult(false);
                result.Errors.Add(errorMessage);

                return result;
            }

            return await manager.SetValue(itemId, parameter, objValue);
        }

        [HttpGet]
        [Route("{execute}/{itemId}/{command}")]
        public async Task<IActionResult> ExecuteGet(string itemId, string command)
        {
            var data = new ExpandoObject() as IDictionary<string, object>;
            foreach (var key in Request.Query.Keys)
            {
                if (Request.Query[key].Count == 1)
                    data.Add(key, Request.Query[key].First());
                else if (Request.Query[key].Count > 1) 
                    data.Add(key, Request.Query[key].Select(v => v).ToList());
            }

            var manager = _fabric.GetApiManager();

            var result = await manager.Execute(itemId, command, data);

            return GetExecutingResult(result);
        }

        [HttpPost]
        [Route("{execute}/{itemId}/{command}")]
        public async Task<IActionResult> ExecutePost(string itemId, string command,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JToken body)
        {
            var data = NewtonsoftHelper.ParseJTokenAsExpando(body);

            var manager = _fabric.GetApiManager();

            var result = await manager.Execute(itemId, command, data);

            return GetExecutingResult(result);
        }

        private IActionResult GetExecutingResult(object result)
        {
            if (result == null)
                return Ok();

            switch (result)
            {
                case ExecuteCommandResultVoid _:
                    return Ok();
                case ExecuteCommandResultFileContent content:
                {
                    return File(content.FileContents, content.ContentType);
                }
                default:
                {
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