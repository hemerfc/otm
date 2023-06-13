using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Otm.Shared.ContextConfig
{
    public class BrokerMessageTypeConfig
    {
        public string Name { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Modes Mode { get; set; }

    }
}