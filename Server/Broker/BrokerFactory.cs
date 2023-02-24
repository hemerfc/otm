using System;
using System.Linq;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Otm.Shared.ContextConfig;
using Otm.Server.Plugin;
using NLog;
using Otm.Server.Broker;
using Otm.Server.Broker.Palantir;
using Otm.Server.Broker.Ptl;
using Otm.Server.Device.Ptl;

namespace Otm.Server.Broker
{
    public static class BrokerFactory
    {
        public static IDictionary<string, IBroker> CreateBrokers(List<BrokerConfig> brokersConfig, ILogger logger)
        {
            var brokers = new Dictionary<string, IBroker>();

            if (brokersConfig != null)
                foreach (var config in brokersConfig)
                {
                    if (string.IsNullOrWhiteSpace(config.Name))
                    {
                        var ex = new Exception("Invalid Broker name in config. Name:" + config.Name);
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    logger.Debug($"Broker {config?.Name}: Intializing with driver '{config.Driver}'");

                    switch (config.Driver)
                    {
                        case "pl":
                            var palantirAmqpBroker = new PalantirAmqpBroker(config, logger);
                            palantirAmqpBroker.Init(config, logger);
                            brokers.Add(config.Name, palantirAmqpBroker);
                            logger.Debug($"Broker {config?.Name}: Created");
                            break;
                        case "ptlSmartPicking":
                            var ptlSmartPickingBroker = new SmartPickingBroker(config, logger);
                            ptlSmartPickingBroker.Init(config, logger);
                            brokers.Add(config.Name, ptlSmartPickingBroker);
                            logger.Debug($"Broker {config?.Name}: Created");
                            break;
                        case "ptlAtop":
                            var ptlAtopBroker = new AtopBroker(config, logger);
                            ptlAtopBroker.Init(config, logger);
                            brokers.Add(config.Name, ptlAtopBroker);
                            logger.Debug($"Broker {config?.Name}: Created");
                            break;
                        default:
                            logger.Debug($"Broker {config?.Name}: Invalid broker type'{config.Driver}'");
                            break;
                    }
                }

            return brokers;
        }
    }
}