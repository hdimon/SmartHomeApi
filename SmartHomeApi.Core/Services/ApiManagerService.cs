using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ApiManagerService : IHostedService, IDisposable
    {
        private readonly IApiManager _apiManager;

        public ApiManagerService(IApiManager itemManager)
        {
            _apiManager = itemManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _apiManager.Initialize();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _apiManager.Dispose();
        }

        public void Dispose()
        {
            _apiManager.Dispose();
        }
    }
}