using System;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using PeterKottas.DotNetCore.WindowsService;
using NLog;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Otm.Config;
using Otm.DataPoint;
using Otm.Transaction;
using Otm.Device;
using Otm.Logger;

namespace Otm
{
    class Program
    {
        static ILogger logger;
        static LoggerFactory loggerFactory;

        static void Main(string[] args)
        {
            loggerFactory = new LoggerFactory();
            logger = loggerFactory.GetCurrentClassLogger();

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
                        var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        var configPath = appPath + "\\config.json";

                        var configString = File.ReadAllText(configPath);                        
                        var rootconfig = JsonConvert.DeserializeObject<RootConfig>(configString);

                        var dataPointFactory = new DataPointFactory();
                        var transactionFactory = new TransactionFactory();
                        var deviceFactory = new DeviceFactory();

                        return new OtmService(controller, 
                            rootconfig, 
                            loggerFactory,
                            dataPointFactory,
                            deviceFactory,
                            transactionFactory);
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
