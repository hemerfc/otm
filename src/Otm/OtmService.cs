using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Otm.Config;
using Otm.DataPoint;
using Otm.Device;
using Otm.Logger;
using Otm.Transaction;
using PeterKottas.DotNetCore.WindowsService.Interfaces;

namespace Otm
{
    public class OtmService : IMicroService
    {
        private ILogger Logger { get; }
        private IMicroServiceController Controller { get; }
        public RootConfig Config { get; }
        public ILoggerFactory LoggerFactory { get; }
        public IDataPointFactory DataPointFactory { get; }
        public IDeviceFactory DeviceFactory { get; }
        public ITransactionFactory TransactionFactory { get; }
        public IDictionary<string, IDataPoint> DataPoints { get; private set; }
        public IDictionary<string, IDevice> Devices { get; private set; }
        public IDictionary<string, ITransaction> Transactions { get; private set; }

        public OtmService(
            IMicroServiceController controller, 
            RootConfig config, 
            ILoggerFactory loggerFactory,
            IDataPointFactory dataPointFactory,
            IDeviceFactory deviceFactory,
            ITransactionFactory transactionFactory)
        {
            this.Controller = controller;
            this.Config = config;
            LoggerFactory = loggerFactory;
            this.Logger = LoggerFactory.GetCurrentClassLogger();

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
            if (Transactions != null)
            {
                foreach (var trans in Transactions.Values)
                {
                    trans.Start();
                }
            }
        }

		public void Stop()
        {
            if (Transactions != null)
            {
                foreach (var trans in Transactions.Values)
                {
                    trans.Stop();
                }
            }
        }
    }
}
