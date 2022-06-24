using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
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
        private IDevice sourceDevice;
        private IDevice targetDevice;
        private IDataPoint dataPoint;
        private DateTime? nextScheduleDateTime;

        public BlockingCollection<Dictionary<string, object>> TriggerQueue { get; private set; }
        public Stopwatch Stopwatch;

        private readonly ILogger logger;

        private bool firstCall = true;

        public Transaction(TransactionConfig trConfig, IDevice sourceDevice, IDevice targetDevice, IDataPoint dataPoint, ILogger logger)
        {
            this.logger = logger;
            this.config = trConfig;
            this.sourceDevice = sourceDevice;
            this.targetDevice = targetDevice;
            this.dataPoint = dataPoint;
            this.TriggerQueue = new BlockingCollection<Dictionary<string, object>>(128);
            Stopwatch = new Stopwatch();
            firstCall = true;
        }



        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            if (config.TriggerType == TriggerTypes.OnTagChange)
            {
                // assina o delagator do datapoint, quando o valor da TriggerSourceName for atualizado
                // dispara o metodo OnTrigger, que coloca o gatilho na TriggerQueue
                sourceDevice.OnTagChangeAdd(config.TriggerTagName, this.OnTrigger);
            }

            while (true)
            {

                try
                {
                    switch (config.TriggerType)
                    {
                        case TriggerTypes.OnTagChange:
                            {
                                Dictionary<string, object> inParams; // = GetInParams();
                                                                     // wait a trigger or 100ms
                                if (TriggerQueue.TryTake(out inParams, 100))
                                    ExecuteTrigger(inParams);
                            }
                            break;
                        case TriggerTypes.OnCycle:
                            {
                                var waitEvent = new ManualResetEvent(false);
                                var inParams = GetInParams();

                                waitEvent.WaitOne(config.TriggerTime); // aguarda XXXms
                                if (sourceDevice.Ready)
                                {
                                    ExecuteTrigger(inParams);
                                }
                            }
                            break;
                        case TriggerTypes.OnScheduler:
                            {
                                if (!nextScheduleDateTime.HasValue)
                                {
                                    nextScheduleDateTime = GetNextScheduleDateTime(dataPoint.CronExpression);
                                    logger.LogInformation($"Next Schedule DateTime for {this.config.Name} is {nextScheduleDateTime}");
                                }
                                else if (nextScheduleDateTime <= DateTimeOffset.UtcNow.UtcDateTime)
                                {
                                    nextScheduleDateTime = GetNextScheduleDateTime(dataPoint.CronExpression);
                                    logger.LogInformation($"Next Schedule DateTime for {this.config.Name} is {nextScheduleDateTime}");

                                    var inParams = GetInParams();
                                    ExecuteTrigger(inParams);
                                }

                                var waitEvent = new ManualResetEvent(false);
                                waitEvent.WaitOne(1000);
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error starting the Transaction {this.config.Name}");
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
            sourceDevice.OnTagChangeRemove(config.TriggerTagName, this.OnTrigger);
        }

        public void OnTrigger(string tagName, Object value)
        {
            // the BlockingCollection does this thread-safe
            var inParams = GetInParams();  //  Dictionary<string, object>
            TriggerQueue.Add(inParams);
        }

        /// <summary>
        /// When a transaction trigger is detected
        /// </summary>
        /// <param name="tagName">trigger tag name</param>
        /// <param name="value">tag value</param>
        public void ExecuteTrigger(Dictionary<string, object> inParams, int retries = 0)
        {
            try
            {
                String str = "";
                foreach (KeyValuePair<string, object> kvp in inParams)
                    str += $"({kvp.Key}:{kvp.Value})";
                logger.LogInformation($"Transaction {config.Name} Input {str}");

                Stopwatch.Restart();

                var outParams = dataPoint.Execute(inParams);

                if (config.TargetBinds is not null) { 
                    foreach (var bind in config.TargetBinds)
                    {
                        var dp = dataPoint.GetParamConfig(bind.DataPointParam);

                        if (!string.IsNullOrWhiteSpace(bind.DeviceTag))
                        {
                            var tag = targetDevice.GetTagConfig(bind.DeviceTag);

                            if (tag.Mode == Modes.FromOTM) // from OTM to device  
                            {
                                targetDevice.SetTagValue(tag.Name, outParams[dp.Name]);
                            }
                        }
                    }                
                }

                var time = Stopwatch.ElapsedMilliseconds;

                str = "";
                foreach (KeyValuePair<string, object> kvp in outParams)
                    str += $"({kvp.Key}:{kvp.Value})";
                logger.LogInformation($"Transaction {config.Name} ({time}ms) Output {str}");
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                if (retries <= 5)
                {
                    logger.LogError(ex, $"Retry no. {retries} of Transaction {this.config.Name}");

                    Thread.Sleep(200);
                    ExecuteTrigger(inParams, ++retries);
                }
                else
                {
                    logger.LogError(ex, $"Retry exceeded Transaction {this.config.Name}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExecuteTrigger of Transaction {this.config.Name}");
            }
        }

        private Dictionary<string, object> GetInParams()
        {
            var inParams = new Dictionary<string, object>();

            if (config.SourceBinds != null) { 
                foreach (var bind in config.SourceBinds)
                {
                    var dp = dataPoint.GetParamConfig(bind.DataPointParam);

                    // if DeviceTag is set, get value from device
                    if (!string.IsNullOrWhiteSpace(bind.DeviceTag))
                    {
                        var tag = sourceDevice.GetTagConfig(bind.DeviceTag);

                        if (tag.Mode == Modes.ToOTM) // from device to OTM  
                        {
                            inParams[dp.Name] = sourceDevice.GetTagValue(tag.Name);
                        }
                    }
                    else // use the static value, provided
                    {
                        /// TODO: only int and string for now...
                        if (dp.TypeCode == TypeCode.Int32)
                            inParams[dp.Name] = Int32.Parse(bind.Value);
                        else if (dp.TypeCode == TypeCode.Byte)
                            inParams[dp.Name] = (byte)Int32.Parse(bind.Value);
                        else if (dp.TypeCode == TypeCode.String)
                            inParams[dp.Name] = bind.Value;
                        else
                            throw new Exception($"Transaction {this.config.Name}: Fixed value only accept int or string!");
                    }
                }
            }

            return inParams;
        }

        private DateTime GetNextScheduleDateTime(string cronExpression)
        {
            var parsedExp = CronExpression.Parse(cronExpression);
            var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
            var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime).Value;

            return occurenceTime;
            // var delay = occurenceTime.GetValueOrDefault() - currentUtcTime;
            // return delay;
        }

        //private TimeSpan WaitForNextSchedule(string cronExpression)
        //{
        //    var parsedExp = CronExpression.Parse(cronExpression);
        //    var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
        //    var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime);

        //    var delay = occurenceTime.GetValueOrDefault() - currentUtcTime;
        //    return delay;
        //}
    }
}