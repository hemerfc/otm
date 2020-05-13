using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Otm.DataPoint;
using Otm.Device;
using Otm.Transaction;
using Otm.ContextConfig;
using Microsoft.Extensions.DependencyInjection;

namespace Otm
{
    public class Program
    {
        private static IConfigService ConfigService;
        public static void Main(string[] args)
        {
            ConfigService = new ConfigService();

            var mng = new OtmContextManager(ConfigService);

            mng.StartAll();

            CreateHostBuilder(args).Build().Run();

            mng.StopAll();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((IServiceCollection services) =>
                {
                    services.AddSingleton<IConfigService>(ConfigService);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
