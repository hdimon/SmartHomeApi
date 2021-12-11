using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Services;
using SmartHomeApi.WebApi.CLI;

namespace SmartHomeApi.WebApi
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();

            var builder = new ConfigurationBuilder();
            
            try
            {
                var config = builder.AddJsonFile("appsettings.json", optional: false).Build();

                if (args is { Length: > 0 })
                {
                    if (args.Length == 1 && args[0] == "cli")
                    {
                        Console.WriteLine("Welcome to SmartHomeApi command line interface!");

                        var code = await new CLIMainMenu().Execute(new CLIContext());

                        if (code == 2)
                        {
                            StartApp(args, config);
                            return 0;
                        }

                        return code;
                    }

                    Console.WriteLine("Invalid argument. Run SmartHomeApi with 'cli' argument to enter CLI mode.");
                    return 1;
                }

                StartApp(args, config);

                return 0;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("appsettings.json not found so run installer.");

                var code = await new CLIMainMenu().Execute(new CLIContext
                    { Options = new Dictionary<string, object> { { "MenuItem", "Create appsettings.json" } } });

                if (code == 2)
                {
                    var config = builder.AddJsonFile("appsettings.json", optional: false).Build();
                    StartApp(args, config);

                    return 0;
                }

                return code;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void StartApp(string[] args, IConfigurationRoot config)
        {
            Console.WriteLine("Press Ctrl+C or Ctrl+Break to shut down");

            AppSettings settings = new AppSettings();
            config.GetSection("AppSettings").Bind(settings);

            if (!string.IsNullOrWhiteSpace(settings.ApiCulture))
            {
                var culture = new CultureInfo(settings.ApiCulture);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureServices(services => { services.AddHostedService<ApiManagerService>(); })
                .UseWindowsService();
    }
}