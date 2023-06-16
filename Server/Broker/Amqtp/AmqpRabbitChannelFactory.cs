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
    public class AmqpRabbitChannelFactory : IAmqpChannelFactory
    {
        public AmqpRabbitChannelWrapper Create(string hostName, int port, string queuesToConsume, string queuesToProduce, EventHandler<BasicDeliverEventArgs> onReceived, ILogger logger)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = hostName, Port = port, AutomaticRecoveryEnabled = true };
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += onReceived;

                var queueNames = queuesToConsume.Split("|");
                foreach (var queueName in queueNames)
                {
                    channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                    channel.BasicConsume(queue: queueName,
                                         autoAck: false,
                                         consumer: consumer);
                }

                queueNames = queuesToProduce.Split("|");
                foreach (var queueName in queueNames)
                {
                    channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
                }

                return new AmqpRabbitChannelWrapper(channel);
            }
            catch
            {
                logger.Warn($"Broker: Error creating amqtp channel {hostName}:{port}");
                return null;
            }
        }
    }

}