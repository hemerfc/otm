using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Otm.Server.ContextConfig
{
    public class TransactionConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DataPointName { get; set; }
        public string SourceDeviceName { get; set; }
        public string TargetDeviceName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TriggerTypes TriggerType { get; set; }
        public string TriggerTagName { get; set; }
        public int TriggerTime { get; set; }
        public List<TransactionBindConfig> SourceBinds { get; set; }
        public List<TransactionBindConfig> TargetBinds { get; set; }
        public string ContextName { get; set; }
    }
}