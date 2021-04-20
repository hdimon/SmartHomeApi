using System;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ApiItemLogger : IApiLogger
    {
        private readonly IApiLogger _apiLogger;
        private readonly string _itemId;

        public ApiItemLogger(IApiLogger apiLogger, string itemId)
        {
            _apiLogger = apiLogger;
            _itemId = itemId;
        }

        public void Prompt(string message)
        {
            _apiLogger.Prompt(GetMessage(message));
        }

        public void Info(string message)
        {
            _apiLogger.Info(GetMessage(message));
        }

        public void Error(Exception ex)
        {
            _apiLogger.Error(ex, GetMessage(ex.Message));
        }

        public void Error(string message)
        {
            _apiLogger.Error(GetMessage(message));
        }

        public void Error(Exception exception, string message)
        {
            _apiLogger.Error(exception, GetMessage(message));
        }

        public void Warning(string message)
        {
            _apiLogger.Warning(GetMessage(message));
        }

        public void Debug(string message)
        {
            _apiLogger.Debug(GetMessage(message));
        }

        private string GetMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(_itemId))
                return message;

            return $"[{_itemId}] {message}";
        }

        public void Dispose()
        {
        }
    }
}