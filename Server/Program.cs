using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Otm.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(builder =>
                {
                    //var sp = builder.Services.BuildServiceProvider();
                    //builder.AddProvider(new SignalrRLoggerProvider(sp));
                    //builder.AddFilter(
                    //  "Microsoft.AspNetCore.SignalR", LogLevel.Trace);
                    //builder.AddFilter(
                    //  "Microsoft.AspNetCore.Http.Connections",
                    //  LogLevel.Trace);
                });
    }
}
