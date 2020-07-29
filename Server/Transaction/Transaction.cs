using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Shared.ContextConfig;

namespace Otm.Server.Transaction
{
    public class Transaction : ITransaction
    {
        public string Name { get { return config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private TransactionConfig config;
        private IDevice device;
        private IDataPoint dataPoint;

        public BlockingCollection<Tuple<string, Object>> TriggerQueue { get; private set; }
        private readonly ILogger logger;

        public Transaction(TransactionConfig trConfig, IDevice device, IDataPoint dataPoint, ILogger logger)
        {
            this.logger = logger;
            this.config = trConfig;
            this.device = device;
            this.dataPoint = dataPoint;
            this.TriggerQueue = new BlockingCollection<Tuple<string, Object>>(128);
        }

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            if (config.TriggerType == TriggerTypes.OnTagChange)
            {
                // assina o delagator do datapoint, quando o valor da TriggerTagName for atualizado
                // dispara o metodo OnTrigger, que coloca o gatilho na TriggerQueue
                device.OnTagChangeAdd(config.TriggerTagName, this.OnTrigger);
            }

            while (true)
            {
                try
                {
                    if (config.TriggerType == TriggerTypes.OnTagChange)
                    {
                        Tuple<string, object> trigger;
                        // wait a trigger or 100ms
                        if (TriggerQueue.TryTake(out trigger, 100))
                            ExecuteTrigger(/*trigger.Item1, trigger.Item2*/);
                    }

                    if (config.TriggerType == TriggerTypes.OnCycle)
                    {
                        var waitEvent = new ManualResetEvent(false);
                        waitEvent.WaitOne(config.TriggerTime); // aguarda XXXms
                        if (device.Ready)
                            ExecuteTrigger(/*trigger.Item1, trigger.Item2*/);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error in ExecuteTrigger of Transaction {this.config.Name}");
                }

                if (Worker.CancellationPending)
                {
                    Stop();
                    return;
                }
            }
        }

        public void Stop()
        {
            device.OnTagChangeRemove(config.TriggerTagName, this.OnTrigger);
        }

        public void OnTrigger(string tagName, Object value)
        {
            // the BlockingCollection does this thread-safe
            TriggerQueue.Add(new Tuple<string, Object>(tagName, value));
        }

        /// <summary>
        /// When a transaction trigger is detected
        /// </summary>
        /// <param name="tagName">trigger tag name</param>
        /// <param name="value">tag value</param>
        public void ExecuteTrigger(/*string tagName, Object value*/)
        {
            var inParams = new Dictionary<string, object>();

            foreach (var bind in config.Binds)
            {
                var dp = dataPoint.GetParamConfig(bind.DataPointParam);
                var tag = device.GetTagConfig(bind.DeviceTag);

                if (tag.Mode == Modes.ToOTM) // from device to OTM  
                {
                    inParams[dp.Name] = device.GetTagValue(tag.Name);
                }
                if (tag.Mode == Modes.Static) // from device to OTM  
                {
                    inParams[dp.Name] = device.GetTagValue(tag.Name);
                }
            }

            var outParams = dataPoint.Execute(inParams);

            foreach (var bind in config.Binds)
            {
                var dp = dataPoint.GetParamConfig(bind.DataPointParam);
                var tag = device.GetTagConfig(bind.DeviceTag);

                if (tag.Mode == Modes.FromOTM) // from OTM to device  
                {
                    device.SetTagValue(tag.Name, outParams[dp.Name]);
                }
            }
        }
    }
}