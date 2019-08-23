using System;
using System.Collections.Generic;
using Otm.Config;
using Otm.DataPoint;
using Otm.Device;

namespace Otm.Transaction
{
    public class Transaction : ITransaction
    {
        private TransactionConfig config;
        private IDevice device;
        private IDataPoint dataPoint;

        public Transaction(TransactionConfig trConfig, IDevice device, IDataPoint dataPoint)
        {
            this.config = trConfig;
            this.device = device;
            this.dataPoint = dataPoint;
        }

        public void Start()
        {
            device.OnTagChangeAdd(config.TriggerTagName, this.OnTrigger);
        }

        public void Stop()
        {
            device.OnTagChangeRemove(config.TriggerTagName, this.OnTrigger);
        }        

        /// <summary>
        /// When a transaction trigger is detected
        /// </summary>
        /// <param name="tagName">trigger tag name</param>
        /// <param name="value">tag value</param>
        public void OnTrigger(string tagName, Object value)
        {
            var inParams = new Dictionary<string, object>();

            foreach(var bind in config.Binds)
            {
                var dp = dataPoint.GetParamConfig(bind.DataPointParam);
                var tag = device.GetTagConfig(bind.DeviceTag);
                
                if (tag.Mode == "in") // input tags are send/writeed to plc  
                {
                    inParams[tag.Name] = device.GetTagValue(tag.Name);
                }
            }
            
            var outParams = dataPoint.Execute(inParams);

            foreach(var bind in config.Binds)
            {
                var dp = dataPoint.GetParamConfig(bind.DataPointParam);
                var tag = device.GetTagConfig(bind.DeviceTag);
                
                if (tag.Mode == "out")
                {
                    device.SetTagValue(tag.Name, outParams[tag.Name]);
                }
            }            

        }
    }
}