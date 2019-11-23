using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class DeviceManagerService : IHostedService, IDisposable
    {
        private readonly IApiManager _deviceManager;

        public DeviceManagerService(IApiManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _deviceManager.Initialize();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _deviceManager.Dispose();
        }

        public void Dispose()
        {
            _deviceManager.Dispose();
        }
    }
}