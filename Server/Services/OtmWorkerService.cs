using Otm.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Otm.Server.ContextConfig;
using NLog;

namespace Otm.Server.Services
{
    public class OtmWorkerService : BackgroundService
    {
        private readonly IConfigService _configService;
        private readonly IStatusService _statusService;
        public readonly OtmContextManager _otmContextManager;

        public OtmWorkerService(IStatusService statusService, IConfigService configService)
        {

            _statusService = statusService;
            _configService = configService;

            _otmContextManager = new OtmContextManager(_configService, _statusService);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //_logger.Info("System START!");

            _otmContextManager.StartAll();

            while (!stoppingToken.IsCancellationRequested)
            {
                // a cada segundo
                await Task.Delay(1000, stoppingToken);
            }

            //_logger.Info("System STOP!");

            _otmContextManager.StopAll();
        }

    }
}