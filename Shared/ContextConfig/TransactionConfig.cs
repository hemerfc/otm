using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Otm.Shared.ContextConfig
{
    public class TransactionConfig
    {
        public string Name { get; set; }
        public string DataPointName { get; set; }
        public string SourceDeviceName { get; set; }
        public string TargetDeviceName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TriggerTypes TriggerType { get; set; }
        public string TriggerSourceName { get; set; }
        public int TriggerTime { get; set; }
        public List<TransactionBindConfig> Binds { get; set; }
    }
}