using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SmartHomeApi.Core;
using SmartHomeApi.Core.DeviceHelpers;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Services;

namespace SmartHomeApi.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISmartHomeApiFabric, SmartHomeApiDefaultFabric>();
            services.AddSingleton<IDevicePluginLocator, DevicePluginLocator>();
            services.AddSingleton<IRequestProcessor, RequestProcessor>();
            services.AddSingleton<IDeviceLocator, DeviceLocator>();
            services.AddSingleton<IDeviceManager, DeviceManager>();
            services.AddSingleton<IDeviceConfigLocator, DeviceConfigLocator>();
            services.AddSingleton<IEventHandlerLocator, EventHandlerLocator>();
            services.AddSingleton<IDeviceHelpersFabric, DeviceHelpersDefaultFabric>();

            services.AddTransient<IDeviceStateStorageHelper, DeviceStateStorageHelper>();

            services.AddControllers();

            services.AddMvc(options =>
                    {
                        /*options.Filters.Add(typeof(LogExceptionAttribute));*/
                    })
                    .AddNewtonsoftJson(options => {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.Formatting = Formatting.Indented;
                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
