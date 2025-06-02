using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otm.Server.ContextConfig;
using Otm.Server.OpenTelemetry;
using Otm.Server.Services;
using Otm.Server.OTel;

namespace Otm.Server
{
    public class Startup
    {
        private readonly IConfiguration _appConfiguration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        private readonly bool _openTelemetryEnabled = false;
        private readonly string _jaegerHost = string.Empty;
        private readonly int _jaegerPort = 0;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigService = new ConfigService();
            ContextService = new ContextService();
            StatusService = new StatusService();
            DeviceService = new DeviceService();
            LogsService = new LogsService();
            TransactionService = new TransactionService();
            
            bool.TryParse(_appConfiguration["OpenTelemetry:IsEnabled"], out _openTelemetryEnabled);
        }

        public IConfiguration Configuration { get; }
        public ContextService ContextService { get; }
        public ConfigService ConfigService { get; }
        public StatusService StatusService { get; }
        public DeviceService DeviceService { get; }
        public LogsService LogsService { get; }
        public TransactionService TransactionService { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IConfigService>(ConfigService);
            services.AddSingleton<IContextService>(ContextService);
            services.AddSingleton<IStatusService>(StatusService);
            services.AddSingleton<IDeviceService>(DeviceService);
            services.AddSingleton<ITransactionService>(TransactionService);
            services.AddSingleton<ILogsService>(LogsService);

            services.AddSingleton<OtmWorkerService>();
            services.AddHostedService(provider => provider.GetService<OtmWorkerService>());

            services.AddOTel("OTM");
            //services.AddHostedService<OtmWorkerService>();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
