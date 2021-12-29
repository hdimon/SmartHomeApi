using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.Services
{
    public class ApiManagerService : IHostedService, IAsyncDisposable
    {
        private readonly IApiManager _apiManager;

        public ApiManagerService(IApiManager itemManager, IApiLogger logger)
        {
            string assemblyVersion = null;

            try
            {
                var assembly = Assembly.GetEntryAssembly();
                var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                assemblyVersion = versionAttribute.InformationalVersion;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            logger.Info($"SmartHomeApi service [{assemblyVersion}] is running...");
            logger.Info($"Minimal supported version of SmartHomeApi.Utils is {PluginsRuntimeSettings.MinimalSupportedVersion}.");

            _apiManager = itemManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _apiManager.Initialize();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _apiManager.DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _apiManager.DisposeAsync();
        }
    }
}