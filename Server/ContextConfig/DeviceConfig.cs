using System;
using System.Collections.Generic;

namespace Otm.Shared.ContextConfig
{
    public class DeviceConfig
    {
        public Guid Id { get; set; }
        public string TipoPtl { get; set; }
        public string Name { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }
        public List<DeviceTagConfig> Tags { get; set; }
        public string ContextName { get; set; }
    }
}