using System;
using System.Linq;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Otm.Server.Plugin;
using Otm.Server.Broker;
using Otm.Server.Broker.Palantir;
using Otm.Server.Broker.Ptl;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Otm.Server.Broker.Amqtp
{
    public interface IAmqpChannelFactory
    {
        AmqpRabbitChannelWrapper Create(string hostName, int port, string queuesToConsume, string queuesToProduce, EventHandler<BasicDeliverEventArgs> onReceived, ILogger logger);
    }
}