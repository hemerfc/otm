using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NLog;
using Otm.ContextConfig;
using Otm.DataPoint;
using Otm.Device;
using Otm.Logger;
using Otm.Transaction;

namespace Otm
{
    public class OtmContext
    {
        public RootConfig Config { get; }
        public IDataPointFactory DataPointFactory { get; }
        public IDeviceFactory DeviceFactory { get; }
        public ITransactionFactory TransactionFactory { get; }
        public IDictionary<string, IDataPoint> DataPoints { get; private set; }
        public IDictionary<string, IDevice> Devices { get; private set; }
        public IDictionary<string, ITransaction> Transactions { get; private set; }
        private static readonly ILogger Logger = LoggerFactory.GetCurrentClassLogger();

        public OtmContext(
            RootConfig config,
            IDataPointFactory dataPointFactory,
            IDeviceFactory deviceFactory,
            ITransactionFactory transactionFactory)
        {
            Config = config;
            DataPointFactory = dataPointFactory;
            DeviceFactory = deviceFactory;
            TransactionFactory = transactionFactory;
        }

        public void Initialize()
        {
            Logger.Info("OTM Initializing...");
            try
            {
                DataPoints = DataPointFactory.CreateDataPoints(Config.DataPoints);
                Devices = DeviceFactory.CreateDevices(Config.Devices);
                Transactions = TransactionFactory.CreateTransactions(Config.Transactions, DataPoints, Devices);
                Logger.Info("OTM Initialized!");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OTM Initialization error!");
                throw ex;
            }
        }

        public void Start()
        {
            Logger.Info($"OTM Serice START requested");

            this.Initialize();

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

            Logger.Info($"OTM Service START completed");
        }

        public void Stop()
        {
            Logger.Info($"OTM Serice STOP requested");

            var workers = new List<BackgroundWorker>();

            workers.AddRange(Devices.Select(s => s.Value.Worker));

            workers.AddRange(Transactions.Select(s => s.Value.Worker));

            foreach (var worker in workers)
                worker?.CancelAsync(); // ? to avoid a complex mock setup

            var busyWaitEvent = new ManualResetEvent(false);

            while (workers.Any(w => (w?.IsBusy) ?? false))
                busyWaitEvent.WaitOne(500); // aguarda 500ms

            Logger.Info($"OTM Serice STOP completed");
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
                        Logger.Error($"Object {name}: Started");

                        StartAction(worker);
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
