using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SmartHomeApi.Core;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.ItemHelpers;
using SmartHomeApi.Core.Services;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.WebApi
{
    public class Startup
    {
        private IServiceProvider _services;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddSingleton<AppSettings>();
            services.AddSingleton<ISmartHomeApiFabric, SmartHomeApiDefaultFabric>();
            services.AddSingleton<IItemsPluginsLocator, ItemsPluginsLocator>();
            services.AddSingleton<IItemsLocator, ItemsLocator>();
            services.AddSingleton<IApiManager, ApiManager>();
            services.AddSingleton<IItemsConfigLocator, ItemsConfigLocator>();
            //services.AddSingleton<IItemHelpersFabric, ItemHelpersDefaultFabric>();
            services.AddSingleton<IApiLogger, ApiLogger>();
            services.AddSingleton<INotificationsProcessor, NotificationsProcessor>();
            services.AddSingleton<IItemStatesProcessor, ItemStatesProcessor>();

            services.AddTransient<IItemStateStorageHelper, ItemStateStorageHelper>();
            services.AddTransient<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddTransient<IUncachedStatesProcessor, UncachedStatesProcessor>();
            services.AddTransient<IDateTimeOffsetProvider, DateTimeOffsetProvider>();

            services.AddControllers();

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new ApiDefaultContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.None
                };
                settings.Converters.Add(new StringEnumConverter());

                return settings;
            };

            services.AddMvc(options =>
                    {
                        /*options.Filters.Add(typeof(LogExceptionAttribute));*/
                    })
                    .AddNewtonsoftJson(options => {
                        options.SerializerSettings.ContractResolver = new ApiDefaultContractResolver();
                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.Formatting = Formatting.Indented;
                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            _services = app.ApplicationServices;

            //applicationLifetime.ApplicationStopped.Register(StopEvent);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandler(a => a.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = feature.Error;

                var result = JsonConvert.SerializeObject(new { error = exception.Message });
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(result);
            }));

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /*private void StopEvent()
        {
            var manager = _services.GetService<IApiManager>();

            manager?.Dispose();
        }*/
    }
}
