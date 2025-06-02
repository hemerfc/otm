using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Otm.Server.ContextConfig;
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
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigService = new ConfigService();
            ContextService = new ContextService();
            StatusService = new StatusService();
            DeviceService = new DeviceService();
            LogsService = new LogsService();
            TransactionService = new TransactionService();
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
