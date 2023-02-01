
using System;
using System.Collections.Generic;

namespace Otm.Server.Broker
{
    public interface IBrokerStatus
    {
        public string Name { get; }
        public bool Enabled { get; }

        public bool Connected { get; }

        public DateTime LastMessageTime { get; }
        public DateTime LastErrorTime { get; }

        public double MessagesPerSecond { get; }
    }
}