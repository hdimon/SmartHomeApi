using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ApiLogger : IApiLogger
    {
        readonly Logger _logger;

        public ApiLogger(IConfiguration configuration)
        {
            _logger = new LoggerConfiguration()
                      .ReadFrom.Configuration(configuration)
                      .Enrich.FromLogContext()
                      .CreateLogger();
        }

        public void Prompt(string message)
        {
            _logger.Verbose(message);
        }

        public void Info(string message)
        {
            _logger.Information(message);
        }

        public void Error(Exception ex)
        {
            _logger.Error(ex, ex.Message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(Exception ex, string message)
        {
            _logger.Error(ex, message);
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
}