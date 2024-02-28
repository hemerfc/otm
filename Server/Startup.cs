using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otm.Server.ContextConfig;
using Otm.Server.OpenTelemetry;
using Otm.Server.Services;

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

            if (_openTelemetryEnabled)
            {
                _jaegerHost = _appConfiguration["Jaeger:Host"];
                
                if(!int.TryParse(_appConfiguration["Jaeger:Port"], out _jaegerPort))
                {
                    throw new Exception("Invalid Jaeger port");
                }
                
            }
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
            //services.AddControllersWithViews();
            //services.AddRazorPages();
            services.AddControllers();
            /*services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "wwwroot";
            });*/
            services.AddSingleton<IConfigService>(ConfigService);
            services.AddSingleton<IContextService>(ContextService);
            services.AddSingleton<IStatusService>(StatusService);
            services.AddSingleton<IDeviceService>(DeviceService);
            services.AddSingleton<ITransactionService>(TransactionService);
            services.AddSingleton<ILogsService>(LogsService);

            services.AddSingleton<OtmWorkerService>();
            services.AddHostedService(provider => provider.GetService<OtmWorkerService>());
            
            if (_openTelemetryEnabled)
            {
                var resource = ResourceBuilder.CreateDefault().AddService(
                    serviceName: TelemetryConstants.ServiceName, 
                    serviceNamespace: TelemetryConstants.ServiceNameSpace,
                    serviceVersion: TelemetryConstants.ServiceVersion
                    );

                services.AddOpenTelemetry()
                    .WithTracing(b =>
                        {
                            b.AddSource(TelemetryConstants.ServiceName);
                            
                            // decorate our service name so we can find it when we look inside Jaeger
                            b.SetResourceBuilder(resource);

                            // receive traces from built-in sources
                            b.AddHttpClientInstrumentation();
                            b.AddAspNetCoreInstrumentation();
                            b.AddSqlClientInstrumentation();

                            b.AddJaegerExporter(e =>
                            {
                                e.AgentHost = _jaegerHost;
                                e.AgentPort = _jaegerPort;
                            });
                        }
                    )
                    .WithMetrics(b =>
                    {
                        // add prometheus exporter
                        b.AddPrometheusExporter();

                        // receive metrics from our own custom sources
                        b.AddMeter(TelemetryConstants.MyAppSource);

                        // decorate our service name so we can find it when we look inside Prometheus
                        b.SetResourceBuilder(resource);

                        // receive metrics from built-in sources
                        b.AddHttpClientInstrumentation();
                        b.AddAspNetCoreInstrumentation();
                    });

            }
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            //app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            if (_openTelemetryEnabled)
            {
                // add the /metrics endpoint which will be scraped by Prometheus
                app.UseOpenTelemetryPrometheusScrapingEndpoint();
            }

            // For the wwwroot folder
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
