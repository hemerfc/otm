using Otm.Server.Device.Ptl;
using Otm.Server.Device.TcpServer;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using NLog;
using System;
using System.Linq.Expressions;
using System.Threading;
using Otm.Server.ContextConfig;

namespace Otm.Server.Broker.Ptl
{
    public abstract class IPtlAmqtpBroker : IBroker
    {
        public ILogger Logger;

        public IModel AmqpChannel { get; set; }

        public BrokerConfig Config;
        public ITcpClientAdapter client;

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
        public bool Connecting { get; private set; }
        public DateTime LastConnectionTry { get; set; }
        public DateTime LastSend { get; private set; } = DateTime.Now;
     
        protected readonly object lockSendDataQueue = new object();
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

        public abstract byte[] GetMessagekeepAlive();

        public void Init(BrokerConfig config, ILogger logger, ITcpClientAdapter tcpClientAdapter = null)
        {
            this.Logger = logger;
            this.Config = config;

            this.client = tcpClientAdapter ?? new TcpClientAdapter();
            this.AmqpChannel = CreateChannel(config.AmqpHostName,
                config.AmqpPort,
                Config.AmqpQueueToConsume,
                Config.AmqpQueueToProduce,
                new EventHandler<BasicDeliverEventArgs>(Consumer_Received)
                );
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

            while (true) {
                try
                {
                    if (client.Connected)
                    {
                        bool received, sent;

                        do
                        {
                            received = ReceiveData();
                            sent = SendData();
                            } while (received || sent);
                        
                            Ready = true;
                    }
                    else
                    {
                        Ready = false;

                        if (!Connecting)
                        {
                            // se ja tiver passado o delay, tenta reconectar
                            if (LastConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                            {
                                LastConnectionTry = DateTime.Now;
                                Connecting = true;
                                //Verifica se consegue conectar
                                Connect();
                                Connecting = false;
                            }
                        }
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
                    Logger.Error($"Dev {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        private IModel CreateChannel(string hostName, int port, string queuesToConsume, string queuesToProduce, EventHandler<BasicDeliverEventArgs> onReceived)
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = hostName,
                Port = port
            };

            IModel channel = null;
            const int maxRetries = 7; // Número máximo de tentativas
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var connection = factory.CreateConnection();
                    channel = connection.CreateModel();
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Erro de conexão: {ex.Message}");
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Logger.Error($"Numero de tentativas {retryCount}");
                        Thread.Sleep(2000 * retryCount);
                    }
                }
            }

            if (channel == null)
            {
                throw new ApplicationException("Não foi possível estabelecer a conexão após várias tentativas.");
            }

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
                    //Logger.Debug($"SendData() Dev {Config.Name}: Sending {message}");

                    client.SendData(obj);

                    this.LastSend = DateTime.Now;
                }
            else
            {
                if (LastSend.AddMinutes(15) < DateTime.Now)
                {
                    client.Dispose();
                    Connect();
                    //var getFwCmd = GetMessagekeepAlive();
                    //client.SendData(getFwCmd);
                    this.LastSend = DateTime.Now;
                }
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
                    Logger.Debug($"Dev {Config.Name}: Connected.");
                }
                else
                {
                    Logger.Error($"Dev {Config.Name}: Connection error.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Dev {Config.Name}: Connection error.");
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
