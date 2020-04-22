using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Services;

namespace SmartHomeApi.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false).Build();

            AppSettings settings = new AppSettings();
            config.GetSection("AppSettings").Bind(settings);

            if (!string.IsNullOrWhiteSpace(settings.ApiCulture))
                CultureInfo.CurrentCulture = new CultureInfo(settings.ApiCulture);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureServices(services => { services.AddHostedService<ApiManagerService>(); })
                .UseWindowsService();
    }
}