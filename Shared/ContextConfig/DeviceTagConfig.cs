using System;
using System.Text.Json.Serialization;

namespace Otm.Shared.ContextConfig
{
    public class DeviceTagConfig
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Rate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Modes Mode { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TypeCode TypeCode { get; set; }
    }
}