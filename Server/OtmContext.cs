using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Server.Transaction;
using Otm.Shared.ContextConfig;

namespace Otm.Server
{
    public class OtmContext
    {
        public RootConfig Config { get; }
        public IDictionary<string, IDataPoint> DataPoints { get; private set; }
        public IDictionary<string, IDevice> Devices { get; private set; }
        public IDictionary<string, ITransaction> Transactions { get; private set; }
        private readonly ILogger Logger;

        public OtmContext(RootConfig config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }

        public void Initialize()
        {
            Logger.LogInformation($"OTM {Config.Name} Context Initializing ...");
            try
            {
                DataPoints = DataPointFactory.CreateDataPoints(Config.DataPoints, Logger);
                Devices = DeviceFactory.CreateDevices(Config.Devices, Logger);
                Transactions = TransactionFactory.CreateTransactions(Config.Transactions, DataPoints, Devices, Logger);
                Logger.LogInformation($"OTM {Config.Name} Context Initialized!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"OTM {Config.Name} Context Initialization error!");
            }
        }

        public void Start()
        {
            Logger.LogInformation($"OTM  {Config.Name} Context starting...");

            try
            {
                if (Devices != null)
                {
                    foreach (var dev in Devices.Values)
                    {
                        BuildWorkerAndStart(dev.Name, dev.Start);
                    }
                }

                if (Transactions != null)
                {
                    foreach (var trans in Transactions.Values)
                    {
                        BuildWorkerAndStart(trans.Name, trans.Start);
                    }
                }

                Logger.LogInformation($"OTM {Config.Name} Context started!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"OTM {Config.Name} Context starting error!");
            }
        }

        public void Stop()
        {
            Logger.LogInformation($"OTM {Config.Name} Context stoping...");

            var workers = new List<BackgroundWorker>();

            workers.AddRange(Devices.Select(s => s.Value.Worker));

            workers.AddRange(Transactions.Select(s => s.Value.Worker));

            foreach (var worker in workers)
                worker?.CancelAsync(); // ? to avoid a complex mock setup

            var busyWaitEvent = new ManualResetEvent(false);

            while (workers.Any(w => (w?.IsBusy) ?? false))
                busyWaitEvent.WaitOne(500); // aguarda 500ms

            Logger.LogInformation($"OTM {Config.Name} Context stoped!");
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
                        Logger.LogError($"Object {name}: Started");

                        StartAction(worker);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Error on start of {name} ");
                    }
                };

                worker.RunWorkerCompleted += (object o, RunWorkerCompletedEventArgs args) => Logger.LogInformation($"Object {name} Stoped");

                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error on start of {name} ");
            }
        }
    }
}
