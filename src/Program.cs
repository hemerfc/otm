using System;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using PeterKottas.DotNetCore.WindowsService;
using NLog;
using System.IO;
using System.Reflection;

namespace Otm
{
    class Program
    {
        static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ServiceSetup();
        }

        static void ServiceSetup()
        {
            ServiceRunner<OtmService>.Run(config =>
            {   
                config.SetName("OTM");
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        var configPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                        var configString = File.ReadAllText(configPath);
                        
                        return new OtmService(controller, configString);
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        service.Stop();
                    });

                    serviceConfig.OnError(ex =>
                    {
                        logger.Error(ex, "QTM Service error!");
                    });
                });
            });            
        }        
    }
}
