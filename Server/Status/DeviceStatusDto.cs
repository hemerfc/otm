using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.Status
{
    public class DeviceStatusDto
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public bool Connected { get; set; }

        public DateTime LastErrorTime { get; set; }

        public IReadOnlyDictionary<string, object> TagValues { get; set; }
    }
}
