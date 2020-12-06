using System;
using System.Collections.Generic;
using System.Linq;

namespace Otm.Shared.ContextConfig
{
    public class OtmContextConfig
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public List<DataPointConfig> DataPoints { get; set; }
        public List<DeviceConfig> Devices { get; set; }
        public List<TransactionConfig> Transactions { get; set; }

    }
}