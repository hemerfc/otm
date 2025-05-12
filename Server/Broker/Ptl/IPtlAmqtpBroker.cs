using Otm.Server.Device.Ptl;
using Otm.Server.Device.TcpServer;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using NLog;
using NLog.Fluent;
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
        
        public BackgroundWorker Worker { get; private set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public bool Connected { get; set; }

        private const int RECONNECT_DELAY = 3000;

        public DateTime LastMessageTime { get; set; }

        public DateTime LastErrorTime { get; set; }

        public double MessagesPerSecond { get; set; }
        public DateTime LastSend { get; private set; } = DateTime.Now;
        
        private bool PtlReady { get; set; }
        
        private DateTime LastPtlConnectionTry { get; set; }
        private DateTime LastPtlReceivedData { get; set; }
        private bool PtlConnecting { get; set; }
        
        private bool RabbitMqReady { get; set; }
        private DateTime LastRabbitConnectionTry { get; set; }
        private bool RabbitMqConnecting { get; set; }
        
        private const int KEEP_ALIVE_TIMEOUT = 10000;
        public bool Ready => RabbitMqReady && PtlReady;
     
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
        
        
        private void ReconnectRabbit()
        {
            RabbitMqConnecting = true;
            try
            {
                if (LastRabbitConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                {
                    LastRabbitConnectionTry = DateTime.Now;
                
                    Logger.Info()
                        .Message("Error")
                        .Property("Class", nameof(IPtlAmqtpBroker))
                        .Property("Method", nameof(ReconnectRabbit))
                        .Property("Config", Config.Name)
                        .Property("Device", Config.SocketHostName)
                        .Property("Erro Code", string.Empty)
                        .Property("Message", "Connected Rabbit")
                        .Property("Date", DateTime.Now)
                        .Write();
                    
                    this.AmqpChannel = CreateChannel(Config.AmqpHostName,
                        Config.AmqpPort,
                        Config.AmqpQueueToConsume,
                        Config.AmqpQueueToProduce,
                        new EventHandler<BasicDeliverEventArgs>(Consumer_Received)
                    );
                    RabbitMqReady = AmqpChannel.IsOpen;
                }
            }
            catch (Exception ex)
            {
                Logger.Error()
                    .Message("Error")
                    .Property("Class", nameof(IPtlAmqtpBroker))
                    .Property("Method", nameof(ReconnectRabbit))
                    .Property("Config", Config.Name)
                    .Property("Device", Config.SocketHostName)
                    .Property("Erro Code", "EX-CN-RBT")
                    .Property("Message", ex.Message)
                    .Property("Date", DateTime.Now)
                    .Write();
                RabbitMqReady = false;
            }
            
            
            RabbitMqConnecting = false;
        }
        
        private void ReconnectPtl()
        {
            try
            {
                if (LastPtlConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                {
                    Logger.Info()
                        .Message("Error")
                        .Property("Class", nameof(IPtlAmqtpBroker))
                        .Property("Method", nameof(ReconnectPtl))
                        .Property("Config", Config.Name)
                        .Property("Device", Config.SocketHostName)
                        .Property("Erro Code", string.Empty)
                        .Property("Message", "Reconnect")
                        .Property("Date", DateTime.Now)
                        .Write();
                    LastPtlConnectionTry = DateTime.Now;
                    
                    PtlConnecting = true;
                    if (client == null)
                    {
                        client = new TcpClientAdapter();
                    }

                    if (client.Connected == false)
                    {
                        Connect();
                    }
                    
                    PtlReady = client.Connected;
                    Logger.Info()
                        .Message("Error")
                        .Property("Class", nameof(IPtlAmqtpBroker))
                        .Property("Method", nameof(ReconnectPtl))
                        .Property("Config", Config.Name)
                        .Property("Device", Config.SocketHostName)
                        .Property("Erro Code", string.Empty)
                        .Property("Message", "Connected PTL")
                        .Property("Date", DateTime.Now)
                        .Write();
                }
            }
            catch (Exception ex)
            {
                Logger.Error()
                    .Message("Error")
                    .Property("Class", nameof(IPtlAmqtpBroker))
                    .Property("Method", nameof(ReconnectPtl))
                    .Property("Config", Config.Name)
                    .Property("Device", Config.SocketHostName)
                    .Property("Erro Code", "EX-CN-PTL") //EX = Exception , CN = Connect, PTL = Ptl
                    .Property("Message", ex.Message)
                    .Property("Date", DateTime.Now)
                    .Write();
                PtlReady = false;
            }
            
            PtlConnecting = false;
        }
        
        public void Start(BackgroundWorker worker)
        {
            Worker = worker;

            while (true)
            {
                try
                {
                    #region PtlReady
                    if(PtlConnecting)
                        continue;
                        
                    if (!PtlReady)
                    {
                        ReconnectPtl();
                        continue;
                    }
                    
                    if (client == null || client?.Connected == false)
                    {
                        PtlReady = false;
                        throw new Exception("Client is null!");
                    }
                    
                    if (client?.Connected == false)
                    {
                        PtlReady = false;
                        throw new Exception("Client is not connected!");
                    }
                        
                    #endregion

                    
                    #region RabbitMqReady
                    
                    
                    if(RabbitMqConnecting)
                        continue;
                    
                    if (!RabbitMqReady)
                    {
                        ReconnectRabbit();
                        //Logger.Info($"Dev {Config.Name}: RabbitMq not ready.");
                        continue;
                    }
                    
                    if(AmqpChannel == null && AmqpChannel?.IsOpen == false)
                    {
                        RabbitMqReady = false;
                        throw new Exception("AmqpChannel is null!");
                    }
                        
                    if(AmqpChannel.IsOpen == false)
                    {
                        RabbitMqReady = false;
                        throw new Exception("AmqpChannel is not connected!");
                    }
                    
                    
                    #endregion
                    
                    if(PtlReady && RabbitMqReady)
                    {
                        bool received, sent;
                        do
                        {
                            received = ReceiveData();
                            sent = SendData();
                        } while (received || sent);

                        if (LastPtlReceivedData.AddMilliseconds(KEEP_ALIVE_TIMEOUT) < DateTime.Now)
                        {
                            LastPtlReceivedData = DateTime.Now;
                            Logger.Error()
                                .Message("Error")
                                .Property("Class", nameof(IPtlAmqtpBroker))
                                .Property("Method", nameof(Start))
                                .Property("Config", Config.Name)
                                .Property("Device", Config.SocketHostName)
                                .Property("Erro Code", "EX-LP-KAT")
                                .Property("Message", "KEEP ALIVE TIMEOUT.")
                                .Property("Date", DateTime.Now)
                                .Write();
                            PtlReady = false;
                        }
                    }
                    
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(50);

                    if (Worker.CancellationPending)
                    {
                        RabbitMqReady = false;
                        PtlReady = false;
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    PtlReady = false;
                    RabbitMqReady = false;
                    
                    Logger.Error()
                        .Message("Error")
                        .Property("Class", nameof(IPtlAmqtpBroker))
                        .Property("Method", nameof(Start))
                        .Property("Config", Config.Name)
                        .Property("Device", Config.SocketHostName)
                        .Property("Erro Code", "EX-LP-LOOP")
                        .Property("Message", ex.Message)
                        .Property("Date", DateTime.Now)
                        .Write();
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

                    client.SendData(obj);

                    this.LastSend = DateTime.Now;
                }
            else
            {
                if (LastSend.AddMinutes(15) < DateTime.Now)
                {
                    client.Dispose();
                    PtlReady = false;
                    this.LastSend = DateTime.Now;
                }
            }

            return sent;
        }

        private void Connect()
        {
            client.Connect(Config.SocketHostName, Config.SocketPort);
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
