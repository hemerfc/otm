using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NLog;
using Otm.ContextConfig;
using Otm.DataPoint;
using Otm.Device;
using Otm.Logger;
using Otm.Transaction;

namespace Otm
{
    public class OtmContextManager
    {
        private readonly Dictionary<string, OtmContext> _contexts;

        public IReadOnlyDictionary<string, OtmContext> Contexts { get { return _contexts; } }

        private readonly IConfigService ConfigService;

        public OtmContextManager(IConfigService configService)
        {
            ConfigService = configService;
            _contexts = new Dictionary<string, OtmContext>();
            // load config files
            var configFiles = ConfigService.GetFiles();

            var dataPointFactory = new DataPointFactory();
            var transactionFactory = new TransactionFactory();
            var deviceFactory = new DeviceFactory();

            foreach (var configFile in configFiles)
            {
                var config = ConfigService.LoadConfig(configFile.Name);
                var otmService = new OtmContext(config, dataPointFactory, deviceFactory, transactionFactory);
                var configName = System.IO.Path.GetFileNameWithoutExtension(configFile.Path);
                _contexts.Add(configName, otmService);
            }
        }

        public void StartAll()
        {
            foreach (var ctx in _contexts.Values)
            {
                ctx.Initialize();
                ctx.Start();
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
