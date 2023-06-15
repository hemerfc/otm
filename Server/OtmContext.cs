using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog;
using Otm.Server.Broker;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Server.Transaction;

namespace Otm.Server
{
    public class OtmContext
    {
        public OtmContextConfig Config { get; }
        public IDictionary<string, IDataPoint> DataPoints { get; private set; }
        public IDictionary<string, IDevice> Devices { get; private set; }
        public IDictionary<string, ITransaction> Transactions { get; private set; }
        public IDictionary<string, IBroker> Brokers { get; private set; }
        private readonly Logger Logger;


        public OtmContext(OtmContextConfig config, Logger logger)
        {
            Config = config;
            Logger = logger;
        }

        public void Initialize()
            {
            Logger.Info($"OTM {Config.Name} Context Initializing ...");
            try
            {
                DataPoints = DataPointFactory.CreateDataPoints(Config.DataPoints, Logger);
                Devices = DeviceFactory.CreateDevices(Config.Devices, Logger);
                Transactions = TransactionFactory.CreateTransactions(Config.Transactions, DataPoints, Devices, Logger);
                Brokers = BrokerFactory.CreateBrokers(Config.Brokers, Logger);
                Logger.Info($"OTM {Config.Name} Context Initialized!");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"OTM {Config.Name} Context Initialization error!");
            }
        }

        public bool Start()
        {
            Logger.Info($"OTM  {Config.Name} Context starting...");
            try
            {
                if (Devices != null)
                {
                    foreach (var dev in Devices.Values)
                    {
                        BuildWorkerAndStart(dev.Name, dev.Start);
                    }
                }

                // wait Devices start
                Thread.Sleep(2000);

                if (Transactions != null)
                {
                    foreach (var trans in Transactions.Values)
                    {
                        BuildWorkerAndStart(trans.Name, trans.Start);
                    }
                }

                // wait Devices start
                Thread.Sleep(2000);

                if (Brokers != null)
                {
                    foreach (var broker in Brokers.Values)
                    {
                        BuildWorkerAndStart(broker.Name, broker.Start);
                    }
                }

                Logger.Info($"OTM {Config.Name} Context started!");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"OTM {Config.Name} Context starting error!");
                return false;
            }
        }

        public bool Stop()
        {
            try
            {

                Logger.Info($"OTM {Config.Name} Context stoping...");

                var workers = new List<BackgroundWorker>();

                workers.AddRange(Devices.Select(s => s.Value.Worker));

                workers.AddRange(Transactions.Select(s => s.Value.Worker));

                foreach (var worker in workers)
                    worker?.CancelAsync(); // ? to avoid a complex mock setup

                var busyWaitEvent = new ManualResetEvent(false);

                while (workers.Any(w => (w?.IsBusy) ?? false))
                    busyWaitEvent.WaitOne(500); // aguarda 500ms

                Logger.Info($"OTM {Config.Name} Context stoped!");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"OTM {Config.Name} Context stoping error!");
                return false;
            }
        }

        private void BuildWorkerAndStart(string name, Action<BackgroundWorker> StartAction)
        {
            try
            {
                BackgroundWorker worker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };

                worker.DoWork += (object o, DoWorkEventArgs args) =>
                {
                    try
                    {
                        Logger.Info($"Object {name}: Started");

                        StartAction(worker);
                        
                        Logger.Info($"Success starting {name} ");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error on start of {name} ");
                    }
                };

                worker.RunWorkerCompleted += (object o, RunWorkerCompletedEventArgs args) => Logger.Info($"Object {name} Stoped");

                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error on start of {name} ");
            }
        }

    }
}
