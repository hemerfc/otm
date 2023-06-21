using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Otm.Server.ContextConfig
{
    public class BrokerMessageTypeConfig
    {
        public string Name { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Modes Mode { get; set; }

    }
}