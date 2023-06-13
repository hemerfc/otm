using System;
using System.Linq;
using Xunit;
using Moq;
using Otm.Server.Device.Ptl;
using Otm.Server.ContextConfig;
using NLog;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Otm.Server.Broker.Palantir;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Collections.Generic;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Otm.Test.Brokers
{
    public class PalantirBrokerTests
    {
        // Testa o recebimento de mensagens vindas do PLC (Socket),
        // estas mensagens devem ser tratadas e encaminhadas para o RabbitMq 
        [Theory]
        [InlineData("\x02K01,PLC01,2019-01-21T13:44:23.455\x03",
            new string[] { "K01,PLC01,2019-01-21T13:44:23.455" })]
        [InlineData("\x02\x02K01,PLC01,2019-01-21T13:44:23.455\x03",
            new string[] { "K01,PLC01,2019-01-21T13:44:23.455" })]
        [InlineData("\x02K02,PLC01,2019\x02K01,PLC01,2019-01-21T13:44:23.000\x03-01-21T13:44:23.455\x03",
            new string[] { "K01,PLC01,2019-01-21T13:44:23.000" })]
        [InlineData("\x02K01,PLC01,2019-01-21T13:44:23.000\x03\x02K02,PLC01,2019-01-21T13:44:23.455\x03",
            new string[] { "K01,PLC01,2019-01-21T13:44:23.000", "K02,PLC01,2019-01-21T13:44:23.455" })]

        public void Receive_Palantir_Messages(string recv, string[] results)
        {
            var waitEvent = new ManualResetEvent(false);
            waitEvent.WaitOne(500);

            var brokerConfig = CreateConfig();

            // lista de mensagens recebidas para conferir no final
            List<string> msgReceved = new List<string>();

            EventHandler<BasicDeliverEventArgs> onReceived = (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var consumer = (sender as IBasicConsumer).Model;
                consumer.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                msgReceved.Add(message);
            };

            // filas que ser�o consumidas
            List<string> consumedQueueNames = new List<string>();
            foreach (var result in results)
            {
                var messageType = result.Split(",").First();
                var queueName = "QF_" + messageType;
                consumedQueueNames.Add(queueName);
            }

            var consumerCTS = new CancellationTokenSource();
            CreateConsumer(consumedQueueNames, brokerConfig.AmqpHostName, brokerConfig.AmqpPort, consumerCTS.Token, onReceived);

            // aguarda 500 ms para garantir que o consumer vai limpar a fila antes de iniciarmos o teste

            waitEvent.WaitOne(500);
            // limpa a lista de mensagens recebidas
            msgReceved.Clear();

            var dataStruct = new DataStruct
            {
                recvBuffer = ASCIIEncoding.ASCII.GetBytes(recv),
                sendBuffer = new byte[0]
            };

            var tcpMock = CreateTcpClientMock(dataStruct);
            var broker = CreateBroker(out BackgroundWorker bgWorker, brokerConfig, dataStruct, tcpMock);

            // inicia o loop do device
            bgWorker.RunWorkerAsync();

            waitEvent.WaitOne(500);

            // para a tread do consumer
            consumerCTS.Cancel();
            // para o loop do device
            bgWorker.CancelAsync();

            // as duas listas precis�o ser igual e ter a mesma ordem
            Assert.True(msgReceved.SequenceEqual(results), "As mensagens enviadas e recebidas n�o s�o iguais!");
        }

        // Teste o envio de mensagens vindas do RabbitMq,
        // estas mensagens devem ser tratadas e encaminhadas para o PLC
        [Theory]
        [InlineData(new string[] { "K01,PLC01,2019-01-21T13:44:23.455" },
            new string[] { "\x02K01,PLC01,2019-01-21T13:44:23.455\x03" })]

        public void Send_Palantir_Messages(string[] recvMessages, string[] outPutMessages)
        {
            var brokerConfig = CreateConfig();

            var dataStruct = new DataStruct
            {
                recvBuffer = new byte[0],
                sendBuffer = new byte[0]
            };

            var tcpMock = CreateTcpClientMock(dataStruct);
            var broker = CreateBroker(out BackgroundWorker bgWorker, brokerConfig, dataStruct, tcpMock);

            // inicia o loop do device
            bgWorker.RunWorkerAsync();

            var waitEvent = new ManualResetEvent(false);
            // aguarda 500 ms para garantir que vai limpar a fila antes de iniciar os testes
            waitEvent.WaitOne(500);
            // limpa o buffer
            dataStruct.recvBuffer = new byte[0];
            dataStruct.sendBuffer = new byte[0];

            var queueToDeclare = new List<string>();
            foreach (var msg in recvMessages)
            {
                var parts = msg.Split(',');
                // queueName = msgType + "_" + msgSource
                var queueName = parts[0] + "_" + parts[1];

                if (!queueToDeclare.Contains(queueName))
                    queueToDeclare.Add(queueName);
            }

            // envia as mensangens para simular o processo normal
            var factory = new ConnectionFactory() { HostName = brokerConfig.AmqpHostName, Port = brokerConfig.AmqpPort };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                foreach (var queueName in queueToDeclare)
                {
                    channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                }

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                foreach (var message in recvMessages)
                {
                    var parts = message.Split(',');
                    // queueName = msgSource + "_" +  msgType
                    var queueName = parts[1] + "_" + parts[0];

                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: "",
                                         routingKey: queueName,
                                         basicProperties: properties,
                                         body: body);

                }

                waitEvent.WaitOne(500);

                // para o loop do device
                bgWorker.CancelAsync();

                var messagesStream = string.Join("", outPutMessages);
                var messagesBuffer = Encoding.UTF8.GetBytes(messagesStream);

                // as duas listas precis�o ser igual e ter a mesma ordem
                Assert.True(messagesBuffer.SequenceEqual(dataStruct.sendBuffer), "As mensagens enviadas e recebidas n�o s�o iguais!");
            }
        }

        private PalantirAmqpBroker CreateBroker(
            out BackgroundWorker backgroundWorker, BrokerConfig config,
            DataStruct dataStruct,
            Mock<ITcpClientAdapter> tcpMock)
        {
            var loggerMock = new Mock<ILogger>();

            var palantirBroker = new PalantirAmqpBroker(config, loggerMock.Object);

            palantirBroker.Init(config, loggerMock.Object, tcpMock.Object);

            var bgWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            bgWorker.DoWork += (object o, DoWorkEventArgs args) =>
            {
                palantirBroker.Start(bgWorker);
            };

            backgroundWorker = bgWorker;

            return palantirBroker;
        }

        private static Mock<ITcpClientAdapter> CreateTcpClientMock(DataStruct dataStruct)
        {
            var tcpMock = new Mock<ITcpClientAdapter>();

            tcpMock.Setup(x => x.Connected).Returns(() => true);

            tcpMock.Setup(x => x.GetData()).Returns(() =>
            {
                // copy data to tmp buffer
                var tmpBuffer = dataStruct.recvBuffer;
                // clear recv Buffer
                dataStruct.recvBuffer = new byte[0];

                return tmpBuffer;
            });

            tcpMock.Setup(x => x.SendData(It.IsAny<byte[]>()))
                .Callback<byte[]>((buffer) =>
                {
                    var tmp = new byte[dataStruct.sendBuffer.Length + buffer.Length];
                    dataStruct.sendBuffer.CopyTo(tmp, 0);
                    buffer.CopyTo(tmp, dataStruct.sendBuffer.Length);
                    dataStruct.sendBuffer = tmp;
                });

            return tcpMock;
        }

        private class DataStruct
        {
            public byte[] sendBuffer;
            public byte[] recvBuffer;
        }

        private BrokerConfig CreateConfig()
        {

            // Preparando ocenario para o teste
            return new BrokerConfig
            {
                Name = "plc01",
                Driver = "pl",
                SocketHostName = "127.0.0.1",
                SocketPort = 3020,
                AmqpHostName = "localhost",
                AmqpPort = 5672,
                AmqpQueueToProduce = "QF_K01|QF_R01|QF_03",
                AmqpQueueToConsume = "PLC01_K02|PLC01_R02"
            };
        }

        private void CreateConsumer(List<string> consumedQueueNames, string hostName, int port, CancellationToken token, EventHandler<BasicDeliverEventArgs> onReceived)
        {
            var rabbitThread = new Thread(() =>
            {
                var factory = new ConnectionFactory() { HostName = hostName, Port = port };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += onReceived;

                    foreach (var queueName in consumedQueueNames)
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

                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(10);
                    }
                }
            });

            rabbitThread.Start();
        }

    }
}

