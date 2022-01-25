using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;

namespace SmartHomeApi.WebApi
{
    public class AppMigrator
    {
        public static async Task<int> Migrate(IApiLogger logger, AppSettings settings)
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            
            if (settings.Version == version) return 0;

            logger.Warning($"appsettings.json version {settings.Version} is not equal to application version {version}. " +
                           "Looking for migrations...");

            return await Migrate(logger, settings, settings.Version, version);
        }

        private static async Task<int> Migrate(IApiLogger logger, AppSettings settings, string fromVersion, string toVersion)
        {
            logger.Info("There are no app migrations, just updating appsettings.json to actual version...");

            return await UpdateAppsettingsFile(logger, fromVersion, toVersion);

            //return 1; //Error
        }

        private static async Task<int> UpdateAppsettingsFile(IApiLogger logger, string fromVersion, string toVersion)
        {
            var appSettingsPath = Path.Combine(GetBasePath(), "appsettings.json");
            var json = await File.ReadAllTextAsync(appSettingsPath);

            //Maybe it would be more proper way to use approach described in https://makolyte.com/csharp-how-to-update-appsettings-json-programmatically/ 
            //but in this case user formatting will be lost. In order to prevent rewriting whole config let's just update it with Regex for now.
            json = Regex.Replace(json, $"\"Version\":\\s*\"{fromVersion}\"", $"\"Version\": \"{toVersion}\"");

            await File.WriteAllTextAsync(appSettingsPath, json);

            logger.Info($"appsettings.json Version has been updated to {toVersion}.");

            return 2;
        }

        private static string GetBasePath()
        {
            return AppContext.BaseDirectory;
        }
    }
}