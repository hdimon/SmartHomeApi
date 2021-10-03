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

                /*string[] names = assembly.GetManifestResourceNames();
                string file = null;

                using (var stream = assembly.GetManifestResourceStream(names[0]))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        file = reader.ReadToEnd();
                    }
                }*/
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            logger.Info($"SmartHomeApi service [{assemblyVersion}] is running...");
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