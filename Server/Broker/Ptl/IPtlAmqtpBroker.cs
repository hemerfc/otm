using Otm.Server.Device.Ptl;
using Otm.Server.ContextConfig;
using Otm.Server.Device.TcpServer;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Elasticsearch.Net;

namespace Otm.Server.Broker.Ptl
{
    public abstract class IPtlAmqtpBroker : IBroker
    {
        public ILogger Logger;

        public IModel AmqpChannel { get; set; }

        public BrokerConfig Config;
        public ITcpClientAdapter client;

        public ConnectionFactory connectionFactory;

        private byte STX = 0x02;
        private byte ETX = 0x03;

        public bool Ready { get; set; }

        public BackgroundWorker Worker { get; private set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public bool Connected { get; set; }

        private const int RECONNECT_DELAY = 3000;

        public DateTime LastMessageTime { get; set; }

        public DateTime LastErrorTime { get; set; }

        public double MessagesPerSecond { get; set; }
        public bool TcpConnecting { get; private set; }
        public bool AmpqConnecting { get; private set; }
        public DateTime LastTcpConnectionTry { get; set; }
        public DateTime LastAmqpConnectionTry { get; set; }
        public DateTime LastSend { get; private set; }

        private readonly object lockSendDataQueue = new object();
        public Queue<byte[]> sendDataQueue;
        public IBasicProperties basicProperties;
        public readonly List<PtlBaseClass> ListaLigados = new List<PtlBaseClass>();
        public byte[] receiveBuffer = new byte[0];

        public IPtlAmqtpBroker(BrokerConfig config, ILogger logger)
        {
            this.Config = config;
            this.Logger = logger;
        }

        public abstract void displaysOn(IEnumerable<PtlBaseClass> listaAcender);
        public abstract void ProcessMessage(byte[] body);
        public abstract bool ReceiveData();

        public void Init(BrokerConfig config, ILogger logger, ITcpClientAdapter tcpClientAdapter = null)
        {
            this.Logger = logger;
            this.Config = config;

            this.client = tcpClientAdapter ?? new TcpClientAdapter();
            this.connectionFactory = CreateConnectionFactory(config.AmqpHostName, config.AmqpPort);
            this.sendDataQueue = new Queue<byte[]>();
        }

        public void Init(BrokerConfig config, ILogger logger)
        {
            Init(config, logger, new TcpClientAdapter());
        }

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            while (true)
            {
                try
                {
                    if (this.AmqpChannel == null || !this.AmqpChannel.IsOpen)
                    {
                        Ready = false;

                        if (!AmpqConnecting)
                        {
                            // se ja tiver passado o delay, tenta reconectar
                            if (LastAmqpConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                            {
                                LastAmqpConnectionTry = DateTime.Now;
                                AmpqConnecting = true;

                                // tenta conectar
                                this.AmqpChannel = CreateChannel(
                                    connectionFactory,
                                    Config.AmqpQueueToConsume,
                                    Config.AmqpQueueToProduce,
                                    new EventHandler<BasicDeliverEventArgs>(Consumer_Received)
                                    );

                                AmpqConnecting = false;
                            }
                        }
                    }

                    if (!client.Connected)
                    {
                        Ready = false;

                        if (!TcpConnecting)
                        {
                            // se ja tiver passado o delay, tenta reconectar
                            if (LastTcpConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                            {
                                LastTcpConnectionTry = DateTime.Now;
                                TcpConnecting = true;
                                // tenta conectar
                                Connect();
                                TcpConnecting = false;
                            }
                        }
                    }

                    if (client.Connected && (this.AmqpChannel != null && this.AmqpChannel.IsOpen))
                    {
                        bool received, sent;

                        do
                        {
                            received = ReceiveData();
                            sent = SendData();
                        } while (received || sent);

                        Ready = true;
                    }

                    // wait 50ms
                    /// TODO: wait time must be equals the minimum update rate of tags
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(50);

                    if (Worker.CancellationPending)
                    {
                        Ready = false;
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Ready = false;
                    Logger.Error($"Broker {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        private ConnectionFactory CreateConnectionFactory(string hostName, int port)
        {
            var factory = new ConnectionFactory() { HostName = hostName, Port = port, AutomaticRecoveryEnabled = true };
            return factory;
        }

        private IModel CreateChannel(ConnectionFactory factory, string queuesToConsume, string queuesToProduce, EventHandler<BasicDeliverEventArgs> onReceived)
        {
            try
            {
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                var consumer = new EventingBasicConsumer(channel);

                basicProperties = channel.CreateBasicProperties();
                basicProperties.Persistent = true;

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

                return channel;
            }
            catch
            {
                Logger.Warn($"Broker {Config.Name}: Error creating amqtp channel {Config.AmqpHostName}:{Config.AmqpPort}");
                return null;
            }
        }

        public void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            //var message = Encoding.UTF8.GetString();

            //sendDataQueue.Enqueue(body);

            var consumer = (sender as IBasicConsumer).Model;
            consumer.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);

            ProcessMessage(body);
        }

        /// <summary>
        /// Varrega o array de bytes e encontra o par de STX e ETX, se tiver dois STX antes de um ETX, desconsidera a primeira parte
        /// </summary>
        /// <param name="strRcvd">String Recebida</param>
        /// <returns>retorna uma tupla com a posicao do STX e do ETX, retorna -1 caso não encontrar</returns>
        private (int stx, int ext) GetMessageDelimiters(byte[] strRcvd)
        {
            int stxPos = -1;
            int extPos = -1;

            int i = 0;
            while (i < strRcvd.Length)
            {
                if (strRcvd[i] == STX)
                {
                    stxPos = i;
                }
                else if (strRcvd[i] == ETX)
                {
                    extPos = i;
                    break;
                }

                i++;
            }

            return (stxPos, extPos);
        }

        public bool SendData()
        {
            var sent = false;

            if (sendDataQueue.Count > 0)
                lock (lockSendDataQueue)
                {
                    var st = new Stopwatch();
                    st.Start();

                    var totalLength = 0;
                    foreach (var it in sendDataQueue)
                    {
                        totalLength += it.Length;
                    }

                    var obj = new byte[totalLength];
                    var pos = 0;
                    while (sendDataQueue.Count > 0)
                    {
                        var it = sendDataQueue.Dequeue();
                        Array.Copy(it, 0, obj, pos, it.Length);
                        pos += it.Length;
                    }

                    var message = Encoding.Default.GetString(obj);

                    client.SendData(obj);

                    st.Stop();

                    Logger.Debug($"Broker {Config.Name}: Enviado {obj.Length} bytes em {st.ElapsedMilliseconds} ms.");

                    this.LastSend = DateTime.Now;
                }
            else
            {
            }

            return sent;
        }

        private void Connect()
        {
            try
            {
                client.Connect(Config.SocketHostName, Config.SocketPort);

                if (client.Connected)
                {
                    Logger.Debug($"Broker {Config.Name}: Connected.");
                }
                else
                {
                    Logger.Error($"Broker {Config.Name}: Connection error.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Broker {Config.Name}: Connection error.");
            }
        }

        public static int SearchBytes(byte[] haystack, byte[] needle)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }
    }
}
