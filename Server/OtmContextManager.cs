using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Server.Transaction;

namespace Otm.Server
{
    public class OtmContextManager
    {
        private readonly Dictionary<string, OtmContext> _contexts;

        private IReadOnlyDictionary<string, OtmContext> Contexts { get { return _contexts; } }

        private readonly IConfigService ConfigService;

        private readonly IStatusService StatusService;

        public OtmContextManager(IConfigService configService, IStatusService statusService, ILogger logger)
        {
            ConfigService = configService;
            StatusService = statusService;

            _contexts = new Dictionary<string, OtmContext>();
            // load config files
            var configFiles = ConfigService.GetAll();

            foreach (var configFile in configFiles)
            {
                var config = ConfigService.Get(configFile.Name);
                //var ColectorService = new ColectorService(config.Name);

                var otmService = new OtmContext(config, logger);
                var configName = System.IO.Path.GetFileNameWithoutExtension(configFile.Path);
                _contexts.Add(configName, otmService);
            }
        }

        public void StartAll()
        {
            foreach (var ctx in _contexts.Values)
            {
                if (ctx.Config.Enabled)
                {
                    ctx.Initialize();
                    ctx.Start();
                }
            }
        }

        public void StopAll()
        {
            foreach (var ctx in _contexts.Values)
            {
                ctx.Stop();
            }
        }
    }
}
