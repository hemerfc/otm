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

namespace Otm.Server.Services
{
    public class OtmWorkerService : BackgroundService
    {
        private readonly ILogger<OtmWorkerService> _logger;
        private readonly IConfigService _configService;
        private readonly IStatusService _statusService;
        private readonly OtmContextManager _otmContextManager;

        public OtmWorkerService(ILogger<OtmWorkerService> logger, IStatusService statusService, IConfigService configService)
        {
            _logger = logger;

            _statusService = statusService;
            _configService = configService;

            _otmContextManager = new OtmContextManager(_configService, _statusService, logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("System START!");

            _otmContextManager.StartAll();

            while (!stoppingToken.IsCancellationRequested)
            {
                // a cada segundo
                await Task.Delay(5000, stoppingToken);


                _logger.LogInformation("BEEEP!");
            }

            _logger.LogInformation("System STOP!");

            _otmContextManager.StopAll();
        }

    }
}