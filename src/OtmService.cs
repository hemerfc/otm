using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Otm.Components;
using Otm.DataPointDrivers;
using Otm.DeviceDrivers;

namespace Otm
{
    public class OtmService : IMicroService
    {
        private ILogger Logger { get; }
        private IMicroServiceController Controller { get; }
        public JObject Config { get; }
        public IDataPointDriver[] DataPoints { get; }
        public IDeviceDriver[] Devices { get; }
        public Transaction[] Transactions { get; }

        public OtmService(string configString)
        {
            Controller = null;
            this.Logger = LogManager.GetCurrentClassLogger();
            this.Config = LoadConfig(configString);
        }

        public OtmService(IMicroServiceController controller, string configString)
        {
            this.Controller = controller;
            this.Logger = LogManager.GetCurrentClassLogger();
            this.Config = LoadConfig(configString);

            this.DataPoints = CreateDataPoints(this.Config);

            this.Devices = CreateDevices(this.Config);

            this.Transactions = CreateTransactions(this.Config);
        }

        public static Transaction[] CreateTransactions(JObject config)
        {
            throw new NotImplementedException();
        }

        public static IDeviceDriver[] CreateDevices(JObject config)
        {
            throw new NotImplementedException();
        }

        public static IDataPointDriver[] CreateDataPoints(JObject config)
        {
            throw new NotImplementedException();
        }

        public static JObject LoadConfig(string configString)
        {
            var config = JToken.Parse(configString);
            return (JObject)config;
        }

        public void Start()
        {
            // ctrl.Start();
        }

		public void Stop()
        {
			// ctrl.Stop();
        }
    }
}
