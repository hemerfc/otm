
using System;
using System.Collections.Generic;

namespace Otm.Server.Device
{
    public interface IDeviceStatus
    {
        public string Name { get; }
        public bool Enabled { get; }

        public bool Connected { get; }

        public DateTime LastErrorTime { get; }

        public IReadOnlyDictionary<string, object> TagValues { get; }
    }
}