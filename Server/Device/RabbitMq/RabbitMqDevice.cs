using System;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using Otm.Server.ContextConfig;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;
using System.Collections.Concurrent;
using Otm.Shared.Status;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Otm.Server.Device.S7
{
    public class RabbitMqDevice : IDevice
    {
        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private DeviceConfig Config;


        public Stopwatch Stopwatch { get; }

        private DateTime? connError = null;
        private ILogger Logger;

        public bool Ready { get; private set; }


        public bool Enabled { get { return true; } }
        bool IDeviceStatus.Connected => throw new NotImplementedException();


        public bool Connected = false;
        private string hostname;
        private string exchange;
        private string exchangeType;
        private string queryFilter;

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }


        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            GetConfig(dvConfig);
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            var cparts = dvConfig.Config.Split(';');
            this.hostname = (cparts.FirstOrDefault(x => x.Contains("hostname=")) ?? "").Replace("hostname=", "").Trim();
            this.exchange = (cparts.FirstOrDefault(x => x.Contains("exchange=")) ?? "").Replace("exchange=", "").Trim();
            this.exchangeType = (cparts.FirstOrDefault(x => x.Contains("exchangeType=")) ?? "").Replace("exchangeType=", "").Trim();
            this.queryFilter = (cparts.FirstOrDefault(x => x.Contains("queryFilter=")) ?? "").Replace("queryFilter=", "").Trim();

        }


        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            while (true)
            {
                try
                {
                    if(Ready)

                        // wait 100ms
                        /// TODO: wait time must be equals the minimum update rate of tags
                        //var waitEvent = new ManualResetEvent(false);
                        //waitEvent.WaitOne(50);

                        if (Worker.CancellationPending)
                    {
                        Ready = false;
                        Stop();
                        return;
                    }

                    //ProcessMessage();

                }
                catch (Exception ex)
                {
                    Ready = false;
                    Logger.LogError($"Dev {Config.Name}: Update Loop Error {ex}");
                    //client.Disconnect();
                }
            }
        }

     
        private void ProcessMessage(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;
            Console.WriteLine(" [x] Received '{0}':'{1}'",
                              routingKey,
                              message);
        }

        public void Stop()
        {
            //
        }

        private void Reconnect()
        {
            

        }


        #region Legacy
        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            throw new NotImplementedException();
        }

        public void OnTagChangeRemove(string tagName, Action<string, object> triggerAction)
        {
            throw new NotImplementedException();
        }

        public bool ContainsTag(string tagName)
        {
            throw new NotImplementedException();
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            throw new NotImplementedException();
        }

        public object GetTagValue(string tagName)
        {
            throw new NotImplementedException();
        }

        public void SetTagValue(string tagName, object value)
        {
            throw new NotImplementedException();
        }

        #endregion Legacy
    }
}

