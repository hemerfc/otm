using System.Collections.Generic;

namespace Otm.ContextConfig
{
    public class DeviceConfig
    {
        public string Name { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }
        public List<DeviceTagConfig> Tags { get; set; }
    }
}