using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otm.Client.Api;
using Microsoft.AspNetCore.Components;
using Otm.Client.ViewModel;
using Otm.Client.Services;

namespace Otm.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddSingleton(sp => new OtmStatusViewModel());
            builder.Services.AddTransient<BlazorTimer>();

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddMyServiceClients(config =>
            {
                config.UseJsonClientSerializer()
                .UseJsonClientDeserializer()
                .UseExistingHttpClient()
                .WithBaseAddress(builder.HostEnvironment.BaseAddress);
            });


            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            await builder.Build().RunAsync();
        }
    }
}
