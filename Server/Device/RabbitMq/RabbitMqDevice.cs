using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using Otm.Server.ContextConfig;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Otm.Server.Device.RabbitMq;
using NLog;

namespace Otm.Server.Device.S7
{
    public class RabbitMqDevice : IDevice
    {

        public RabbitMqDevice()
        {
            tagValues = new ConcurrentDictionary<string, object>();
            tagsAction = new ConcurrentDictionary<string, Action<string, object>>();
        }

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private DeviceConfig Config;


        public Stopwatch Stopwatch { get; }

        private ILogger Logger;

        public bool Ready { get; private set; }


        public bool Enabled { get { return true; } }
        bool IDeviceStatus.Connected => Connected; //throw new NotImplementedException();


        public bool Connected = false;
        public bool Connecting = false;
        public DateTime lastConnectionTry = DateTime.Now;
        public int RECONNECT_DELAY = 3000;

        private readonly ConcurrentDictionary<string, object> tagValues;
        private readonly ConcurrentDictionary<string, Action<string, object>> tagsAction;
        public EventingBasicConsumer consumer { get; set; }

        private string hostname;
        private string exchange;
        private string port;
        private string routingKey = "*";

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public IConnection RabbitConnection { get; private set; }
        public IModel RabbitChannel { get; private set; }
        public string UniqueDeviceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? LastUpdateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int LicenseRemainingHours { get ; set ; }

        public object tagsActionLock = new object();

        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            GetConfig(dvConfig);
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            GetDeviceParameter(dvConfig);
            GetDeviceTags(dvConfig);
        }

        private void GetDeviceParameter(DeviceConfig dvConfig)
        {
            try
            {
                var cparts = dvConfig.Config.Split(';');

                this.hostname = (cparts.FirstOrDefault(x => x.Contains("hostname=")) ?? "").Replace("hostname=", "").Trim();
                this.exchange = (cparts.FirstOrDefault(x => x.Contains("exchange=")) ?? "").Replace("exchange=", "").Trim();
                this.port = (cparts.FirstOrDefault(x => x.Contains("port=")) ?? "").Replace("port=", "").Trim();
            }
            catch (Exception ex)
            {
                Logger.Error($"RabbitMqDevice|GetDeviceParameter|Device: {Config.Name}| {ex}");
                throw;
            }
        }
        private void GetDeviceTags(DeviceConfig dvConfig)
        {
            try
            {
                //routingKey = dvConfig.Tags.FirstOrDefault(x => x.Name == nameof(routingKey)).Name ?? "*";
            }
            catch (Exception ex)
            {
                Logger.Error($"RabbitMqDevice|GetDeviceTags|Device: {Config.Name}| {ex}");
                throw;
            }
        }

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            while (true)
            {
                try
                {
                    if (RabbitConnection?.IsOpen ?? false)
                    {
                    }
                    else
                    {
                        if (!Connecting)
                        {
                            // se ja tiver passado o delay, tenta reconectar
                            if (lastConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                            {
                                lastConnectionTry = DateTime.Now;
                                Connecting = true;

                                try
                                {

                                    ConfigureConnection();
                                }
                                catch (Exception ex)
                                {
                                    Ready = false;
                                    //RabbitConnection.Dispose();
                                    Logger.Error($"RabbitMqDevice|Start|Dev {Config.Name}: ConfigureConnection Error {ex}");
                                    //client.Disconnect();
                                }

                                Connecting = false;
                            }
                        }
                    }
                    
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
                    //RabbitConnection.Dispose();
                    Logger.Error($"RabbitMqDevice|Start|Dev {Config.Name}: Update Loop Error {ex}");
                    //client.Disconnect();
                }
            }
        }

        
        private bool ConfigureConnection()
        {
            var valueFound = false;

            var factory = new ConnectionFactory() { HostName = hostname, Port = int.Parse(port) };
            factory.UserName = "aguia";
            factory.Password = "aguia2022";
            RabbitConnection = factory.CreateConnection();

            RabbitChannel = RabbitConnection.CreateModel();
            
            RabbitChannel.ExchangeDeclare(exchange: exchange, type: "topic");
            var queueName = RabbitChannel.QueueDeclare().QueueName;

            RabbitChannel.QueueBind(queue: queueName, exchange: exchange, routingKey: routingKey);

            Logger.Debug($"RabbitMqDevice|ReceiveData|Dev {Config.Name}: Ready for messages.");

            consumer = new EventingBasicConsumer(RabbitChannel);

            consumer.Received += (object model, BasicDeliverEventArgs ea) => processMessage(model, ea, ref valueFound);

            RabbitChannel.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer);

            return valueFound;
        }

        private void processMessage(object model, BasicDeliverEventArgs ea, ref bool valueFound)
        {
            var tagTriggers = new List<(Action<string, object> func, string tagName, object tagValue)>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Logger.Debug($"RabbitMqDevice|processMessage|routingKey: '{routingKey}'| message: '{message}'");

            //Desserialize RabbitMessage
            var rabbitMessage = JsonSerializer.Deserialize<RabbitMessage>(message);

            //Usa reflection para pegar os FieldInfos da RabbitMessage
            foreach (var field in typeof(RabbitMessage).GetProperties())
            {
                //Monta o Tag Name de acordo com as informa��es, obtendo o valor via reflection
                var tagName = $"{exchange}.{ea.RoutingKey}.{field.Name}";
                //Obtem o nome do campo via reflection
                SetTagValue(tagName, field.GetValue(rabbitMessage));

                //Se as tags possuem action
                if (tagsAction.ContainsKey(tagName))
                {
                    // guarda o trigger para executar apos atualizar todos os valores
                    tagTriggers.Add(new(tagsAction[tagName], tagName, tagValues[tagName]));
                }
            }

            valueFound = true;

            foreach (var tt in tagTriggers)
            {
                lock (tagsActionLock)
                {
                    tt.func(tt.tagName, tt.tagValue);
                }
            }
        }

        public void Stop()
        {
            //
        }

        #region Legacy

        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            var tagConfig = GetTagConfig(tagName);

            // can't use a output tag as trigger, output put tags are writed to PLC
            if (tagConfig.Mode == Modes.FromOTM) // from OTM to device
            {
                throw new Exception("Error can't put a trigger on a input tag");
            }
            if (!tagsAction.ContainsKey(tagName))
                tagsAction[tagName] = triggerAction;
            else
                tagsAction[tagName] += triggerAction;
        }

        public void OnTagChangeRemove(string tagName, Action<string, object> triggerAction)
        {
            tagsAction[tagName] -= triggerAction;
        }

        public bool ContainsTag(string tagName)
        {
            return Config.Tags.Any(x => x.Name == tagName);
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            return tagValues[tagName];
        }

        public void SetTagValue(string tagName, object value)
        {
            tagValues[tagName] = value;
            Logger.Debug($"RabbitMqDevice|SetTagValue|TagName: '{tagName}'|TagValues: '{value}'"); 
        }

        public void GetLicenseRemainingHours()
        {
            LicenseRemainingHours = int.MaxValue;
        }

        #endregion Legacy
    }
}

