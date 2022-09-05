using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Server.Transaction;

namespace Otm.Server
{
    public class OtmContextManager
    {
        private readonly Dictionary<string, OtmContext> _contexts;

        public IReadOnlyDictionary<string, OtmContext> Contexts { get { return _contexts; } }

        private readonly IConfigService ConfigService;

        private readonly IStatusService StatusService;

        public OtmContextManager(IConfigService configService, IStatusService statusService)
        {
            ConfigService = configService;
            StatusService = statusService;

            StatusService.SetOtmContextManager(this);

            _contexts = new Dictionary<string, OtmContext>();
            // load config files
            var configFiles = ConfigService.GetAll();

            foreach (var configFile in configFiles)
            {
                var config = ConfigService.Get(configFile.Name);
                //var ColectorService = new ColectorService(config.Name);

                var Newlogger = LogManager.GetLogger(config.LogName);

                var otmService = new OtmContext(config, Newlogger);
                var configName = System.IO.Path.GetFileNameWithoutExtension(configFile.Path);
                _contexts.Add(configName, otmService);
            }
        }

        public void AddNewContext(string name, Logger logger) {
            var config = ConfigService.Get(name);
            var configFile = ConfigService.GetByName(name);
            var configName = System.IO.Path.GetFileNameWithoutExtension(configFile.Path);
            var otmService = new OtmContext(config, logger);
            _contexts.Add(configName, otmService);
        }

        public void InitializeNewContext(string name) {
            var ctx = _contexts.Values.Where(x => x.Config.Name == name);
            ctx.First().Initialize();
            ctx.First().Start();
        }

        public bool ContextExist(string name) {
            var ctx = _contexts.Values.Where(x => x.Config.Name == name);
            return ctx.Count() > 0 ? true : false;
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
