using Jint.Parser;
using Nest;
using Newtonsoft.Json;
using NLog;
using Otm.Server.Device;
using Otm.Server.Device.Ptl;
using Otm.Server.Device.TcpServer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Otm.Server.ContextConfig;

namespace Otm.Server.Broker.Palantir
{
    public class PalantirAmqpBroker : IBroker
    {

        public PalantirAmqpBroker(BrokerConfig config, ILogger logger)
        {
            this.Config = config;
            this.Logger = logger;
        }

        private ILogger Logger;

        public IModel AmqpChannel { get; set; }

        private BrokerConfig Config;
        private ITcpClientAdapter client;

        private byte STX = 0x02;
        private byte ETX = 0x03;

        //private byte[] STX = new byte[] { 0x02 }; 
        //private byte[] ETX = new byte[] { 0x03 };

        
        
        
        private bool PlcReady { get; set; }
        private DateTime LastPlcConnectionTry { get; set; }
        private DateTime LastPlcReceivedData { get; set; }
        private bool PlcConnecting { get; set; }
        
        private bool RabbitMqReady { get; set; }
        private DateTime LastRabbitConnectionTry { get; set; }
        private bool RabbitMqConnecting { get; set; }
        
        
        //necessario implementar o Ready por conta da interface
        public bool Ready => RabbitMqReady && PlcReady;
        
        
        

        public BackgroundWorker Worker { get; private set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        //public bool Connected { get; set; }

        private const int RECONNECT_DELAY = 3000;
        private const int KEEP_ALIVE_TIMEOUT = 10000;

        public DateTime LastMessageTime { get; set; }
        
        public DateTime LastSendK01 { get; set; }

        public DateTime LastErrorTime { get; set; }

        public double MessagesPerSecond { get; set; }
        //public bool ClientConnecting { get; private set; }
        //public DateTime LastConnectionTry { get; set; }
        public DateTime LastSend { get; private set; }

        private byte[] receiveBuffer = new byte[0];
        private readonly object lockSendDataQueue = new object();
        private Queue<byte[]> sendDataQueue;
        private IBasicProperties basicProperties;

        public bool Connected { get { return client?.Connected ?? false; } }

        public void Init(BrokerConfig config, ILogger logger, ITcpClientAdapter tcpClientAdapter = null)
        {
            this.Logger = logger;
            this.Config = config;
            this.Name = config.Name;

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

            while (true)
            {
                try
                {
                    #region PlcReady
                    if(PlcConnecting)
                        continue;
                        
                    if (!PlcReady)
                    {
                        //Logger.Info($"Dev {Config.Name}: PLC not ready.");
                        ReconnectPlc();
                        continue;
                    }
                    
                    if (client == null || client?.Connected == false)
                    {
                        PlcReady = false;
                        throw new Exception("Client is null!");
                    }
                    
                    if (client?.Connected == false)
                    {
                        PlcReady = false;
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
                    
                    if(PlcReady && RabbitMqReady)
                    {
                        bool received, sent;
                        do
                        {
                            received = ReceiveData();
                            sent = SendData();
                        } while (received || sent);

                        if (LastPlcReceivedData.AddMilliseconds(KEEP_ALIVE_TIMEOUT) < DateTime.Now)
                        {
                            LastPlcReceivedData = DateTime.Now;
                            Logger.Warn($"Dev {Config.Name}: KEEP_ALIVE_TIMEOUT. Setting PlcReady to false...");
                            PlcReady = false;
                        }
                    }

                    // wait 50ms
                    /// TODO: wait time must be equals the minimum update rate of tags
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(50);

                    if (Worker.CancellationPending)
                    {
                        RabbitMqReady = false;
                        PlcReady = false;
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    PlcReady = false;
                    RabbitMqReady = false;
                    Logger.Error($"Dev {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        private void ReconnectRabbit()
        {
            RabbitMqConnecting = true;
            try
            {
                if (LastRabbitConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                {
                    LastRabbitConnectionTry = DateTime.Now;
                
                    //if (AmqpChannel == null)
                    //{
                    Logger.Info($"Dev {Config.Name}: Reconnecting to RabbitMq.");
                    this.AmqpChannel = CreateChannel(Config.AmqpHostName,
                        Config.AmqpPort,
                        Config.AmqpQueueToConsume,
                        Config.AmqpQueueToProduce,
                        new EventHandler<BasicDeliverEventArgs>(Consumer_Received)
                    );
                    //}
                    RabbitMqReady = AmqpChannel.IsOpen;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Dev {Config.Name}: Error ReconnectRabbit: {ex}");
                RabbitMqReady = false;
            }
            
            
            RabbitMqConnecting = false;
        }

        private void ReconnectPlc()
        {
            try
            {
                if (LastPlcConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                {
                    LastPlcConnectionTry = DateTime.Now;
                    
                    PlcConnecting = true;
                    if (client == null)
                    {
                        client = new TcpClientAdapter();
                    }

                    if (client.Connected == false)
                    {
                        Logger.Info($"Dev {Config.Name}: Reconnecting to PLC.");
                        Connect();
                    }
                    
                    PlcReady = client.Connected;
                    Logger.Info($"Dev {Config.Name}: PlcReady: {PlcReady}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Dev {Config.Name}: Error ReconnectPlc: {ex}");
                PlcReady = false;
            }
            
            PlcConnecting = false;
        }
        
        

        public void Stop()
        {
            throw new NotImplementedException();
        }


        private bool ReceiveData()
        {
            var received = false;

           // 456,2024-02-02<ETX><STX>R01,23,667
            var recv = client.GetData();
            
            if (recv != null && recv.Length > 0)
            {
                //Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
                //<STX>R01,22,456,2024-02-02<ETX><STX>R01,23,667
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;

                LastPlcReceivedData = DateTime.Now;
            }

            if (receiveBuffer.Length > 0)
            {
                var strRcvd = receiveBuffer;

                var (stxPos, etxPos) = GetMessageDelimiters(strRcvd);

                // se tem um STX
                if (stxPos >= 0)
                {
                    // se tem um ETX
                    if (etxPos >= 0)
                    {
                        if (etxPos > stxPos)
                        {
                            // a mensagem parece estar ok, não precisa fazer nada pode seguir em diante
                        }
                        else
                        {
                            // o STX esta antes do ETX, o que tem antes do STX precisa ser descartado...
                            receiveBuffer = receiveBuffer[(stxPos)..];
                            // envia true para processar novamente sem sleep..
                            return true;
                        }
                    }
                    // se tem um STX mas não tem ETX
                    else
                    {
                        // se encontrou apenas um STX em uma posição maior que 0, 
                        if (stxPos > 0)
                        {
                            
                            receiveBuffer = receiveBuffer[(stxPos)..];
                        }
                        // retorna false para aguardar o restante da mensagem chegar
                        return false;
                    }
                }
                else if (etxPos >= 0)
                {
                    // se encontrou apenas um ETX descarta toda esta parta do buffer
                    //<STX>R01,23,667
                    receiveBuffer = receiveBuffer[(etxPos + 1)..];
                    // envia true para processar novamente sem sleep..
                    return true;
                }
                // se não encontrou nenum STX nem ETX, e tem algo no buffer, descarta
                else if (receiveBuffer.Length > 0)
                    receiveBuffer = Array.Empty<byte>();

                // envia true para processar novamente sem sleep..
                received = true;
                try
                {
                    
                    // pega a mensagem do buffer
                    //<STX>R01,PLC01,22,456,2024-02-02<ETX><STX>R01,23,667
                    //<STX>P01,EST01,22,456,2024-02-02<ETX><STX>R01,23,667
                    var message = strRcvd[(stxPos + 1)..etxPos];
                    //Descartando a mensagem do buffer pois ja foi processada
                    receiveBuffer = receiveBuffer[(etxPos + 1)..];
                    var messageStr = Encoding.ASCII.GetString(message);
                    string queueName;

                    if (!Config.QueueLegacyFormat)
                    {
                        var messageType = messageStr.Split(",")[0];
                        var messageOrigem = messageStr.Split(",")[1];
                        
                        queueName = messageOrigem;
                        
                        //Caso a mensagem seja do próprio drive, o nome da fila é o nome do drive + o tipo da mensagem para evitar conflitos de nomes de filas
                        if (Config.Name == messageOrigem)
                        {
                            queueName = $"{messageOrigem}{messageType}";
                        }
                    }
                    else
                    {
                        var messageType = messageStr.Split(",").First();
                        queueName = messageType;
                    }
                    
                    Logger.Info($"ReceiveData(): Drive: {Config.Name}. Message: {messageStr}");
                    var json = JsonConvert.SerializeObject(new { Body = messageStr });


                    AmqpChannel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));
                }
                catch (Exception e)
                {
                    throw;
                }

            }

            return received;
        }

        private static int SearchBytes(byte[] haystack, byte[] needle)
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

        /// <summary>
        /// Varrega o array de bytes e encontra o par de STX e ETX, se tiver dois STX antes de um ETX, desconsidera a primeira parte
        /// </summary>
        /// <param name="strRcvd">String Recebida</param>
        /// <returns>retorna uma tupla com a posicao do STX e do ETX, retorna -1 caso não encontrar</returns>
        /// //<STX>R01,22,456,2024-02-02<ETX><STX>R01,23,667
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
                            if (it == null)
                            {
                                Logger.Error($"SendData: {Config.Name}: it was NULL");
                                continue;
                            }

                            totalLength += it.Length;
                        }

                        Logger.Info($"SendData: {Config.Name}: totalLength {totalLength}");
                        var obj = new byte[totalLength];
                        var pos = 0;
                        
                        while (sendDataQueue.Count > 0)
                        {
                            var it = sendDataQueue.Dequeue();
                            Array.Copy(it, 0, obj, pos, it.Length);
                            pos += it.Length;
                        }

                        var message = Encoding.Default.GetString(obj);
                        Logger.Info($"SendData: {Config.Name}: MessageJson {message}");

                        string pattern = @"\{[^{}]+\}";

                        Regex regex = new Regex(pattern);

                        MatchCollection matches = regex.Matches(message);

                        Logger.Info($"SendData: {Config.Name}: MatchCollection {matches}");
                        foreach (Match match in matches)
                        {
                            Logger.Info($"SendData: {Config.Name}: Message {match}");
                            Match conteudo = Regex.Match(match.ToString(), @"""body"":\s*""([^""]*)""");

                            // Adiciona o byte 2 no início da mensagem
                            byte[] startByte = new byte[] { 2 };
                            byte[] messageStart = startByte.Concat(Encoding.Default.GetBytes(conteudo.Groups[1].Value)).ToArray();

                            // Adiciona o byte 3 no final da mensagem
                            byte[] endByte = new byte[] { 3 };
                            byte[] messageBytes = messageStart.Concat(endByte).ToArray();


                            Logger.Info($"SendData: {Config.Name}: Client {client.Connected}");
                            client.SendData(messageBytes);
                            
                            Logger.Info($"SendData():    Drive: {Config.Name}: MessageSendToDevice: {conteudo.Groups[1].Value}");
                        }
                        
                        this.LastSend = DateTime.Now;
                }
            else
            {
                if (LastSend.AddSeconds(10) < DateTime.Now)
                {
                    var getFwCmd = new byte[] { 2,3 };
                    client.SendData(getFwCmd);
                    this.LastSend = DateTime.Now;
                }
            }

            return sent;
        }

        private void Connect()
        {
            try
            {
                
                Logger.Debug($"Dev {Config.Name}: Conecting to '{Config.SocketHostName}:{Config.SocketPort}'.");
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

        private IModel CreateChannel(string hostName, int port, string queuesToConsume, string queuesToProduce, EventHandler<BasicDeliverEventArgs> onReceived)
        {
            var factory = new ConnectionFactory() { HostName = hostName, Port = port };
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

        public void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            
            // lock para evitar concorrencia na fila
            lock (lockSendDataQueue)
            {
                sendDataQueue.Enqueue(body);
            }
            
            var consumer = (sender as IBasicConsumer).Model;
            consumer.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);

        }
    }
}
