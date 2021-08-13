using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Otm.Shared.ContextConfig
{
    public class TransactionConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DataPointName { get; set; }
        public string DeviceName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TriggerTypes TriggerType { get; set; }
        public string TriggerTagName { get; set; }
        public int TriggerTime { get; set; }
        public List<TransactionBindConfig> Binds { get; set; }
        public string ContextName { get; set; }
    }
}