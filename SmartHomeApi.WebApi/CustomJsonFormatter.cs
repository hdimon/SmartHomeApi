using System;
using System.Buffers;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SmartHomeApi.WebApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomJsonFormatter : ActionFilterAttribute
    {
        private readonly string _escapeNonAsciiParameter = "EscapeNonAscii";

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context?.Result == null || context.HttpContext?.Request == null ||
                !context.HttpContext.Request.Query.ContainsKey(_escapeNonAsciiParameter) ||
                !context.HttpContext.Request.Query.TryGetValue(_escapeNonAsciiParameter, out var value))
            {
                return;
            }

            if (value.Count != 1 || !bool.TryParse(value.First(), out bool escapeNonAscii))
                return;

            var settings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            settings.ContractResolver = new ApiDefaultContractResolver();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.Converters.Add(new StringEnumConverter());
            settings.Formatting = Formatting.Indented;

            if (escapeNonAscii)
                settings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;

            var formatter = new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions());

            (context.Result as ObjectResult)?.Formatters.Add(formatter);
        }
    }
}