using Otm.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Otm.Server.ContextConfig;
using NLog;
using Otm.Server.Device.Licensing;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otm.Server.OpenTelemetry;

namespace Otm.Server.Services
{
    public class OtmWorkerService : BackgroundService
    {
        private readonly IConfigService _configService;
        private readonly ILogger _logger;
        private readonly IStatusService _statusService;
        public readonly OtmContextManager _otmContextManager;
        private readonly Tracer _tracer;
        
        private readonly bool _openTelemetryEnabled = false;
        private readonly string _jaegerHost = string.Empty;
        private readonly int _jaegerPort = 0;
        
        private readonly IConfiguration _appConfiguration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        public string HostIdentifier { get; }
        public string DeviceIdentifier { get; }

        #region License
        private int LicenseRemainingHours { get; set; }
        private DateTime? LastUpdateDate { get; set; }
        private DateTime? LastLicenseTry;
        private string licenseKey = "";
        #endregion

        public OtmWorkerService(IStatusService statusService, IConfigService configService, TracerProvider provider)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _statusService = statusService;
            _configService = configService;

            _otmContextManager = new OtmContextManager(_configService, _statusService);
            HostIdentifier = Environment.MachineName;
            //Temporariamente pegando o Ip, deve-se tentar pegar algo imutavel do device
            DeviceIdentifier = Dns.GetHostEntry(HostIdentifier).AddressList[0].ToString();
            // this.licenseKey = (cparts.FirstOrDefault(x => x.Contains("key=")) ?? "").Replace("key=", "").Trim();
            
            var resource = ResourceBuilder.CreateDefault().AddService("OtmWorkerService").Build();

            
            bool.TryParse(_appConfiguration["OpenTelemetry:IsEnabled"], out _openTelemetryEnabled);

            if (_openTelemetryEnabled)
            {
                _jaegerHost = _appConfiguration["Jaeger:Host"];
                
                if(!int.TryParse(_appConfiguration["Jaeger:Port"], out _jaegerPort))
                {
                    throw new Exception("Invalid Jaeger port");
                }
                
            }
            
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(TelemetryConstants.MyAppSource)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OtmWorkerService"))
                .AddJaegerExporter(jaegerOptions =>
                {
                    jaegerOptions.AgentHost = _jaegerHost;
                    jaegerOptions.AgentPort = _jaegerPort;
                })
                .Build();

            
            _tracer = tracerProvider.GetTracer(TelemetryConstants.MyAppSource);
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
             _logger.Info("System START!");

            _otmContextManager.StartAll();
            
            using var activity = _tracer.StartSpan("OtmWorkerService.ExecuteAsync");


            while (!stoppingToken.IsCancellationRequested)
            {
                // a cada segundo
                await Task.Delay(1000, stoppingToken);
            }

            _logger.Info("System STOP!");

            _otmContextManager.StopAll();
        }


        public void GetLicenseRemainingHours()
        {
            /*Atualiza quando:
             * - Na inicializa��o do servi�o
             * - Quando a data da �ltima atualiza��o tiver 3 dias de diferen�a da �ltima. Implementei com Mod, caso for alterada a data do servidor para frente, tamb�m funciona
             */

            //Tempor�rio at� corrigir o container
            //LicenseRemainingHours = int.MaxValue;

            if (LastLicenseTry == null
                || LastUpdateDate == null
                || Math.Abs((LastUpdateDate.Value - DateTime.Now).TotalDays) >= 3
                )
            {
                try
                {
                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | Obtendo licenca...");

                    string LicenseKey = this.licenseKey;

                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | HostIdentifier: {HostIdentifier}");
                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | MacAddress: {DeviceIdentifier}");
                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | LicenseKey {LicenseKey}");

                    LicenseRemainingHours = new License(HostIdentifier, DeviceIdentifier, LicenseKey).GetRemainingHours();

                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | Licenca obtida, restante {LicenseRemainingHours} horas.");

                    LastUpdateDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    _logger.Info($"PtlDevice | OtmWorkerService | GetLicenseRemainingDays | Error:  {ex}");
                }
                LastLicenseTry = DateTime.Now;
            }
        }

    }
}