using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.Status
{
    public class ContextStatusDto
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public IDictionary<string, DeviceStatusDto> DeviceStatus { get;set;}
        public IDictionary<string, DataPointStatusDto> DataPointStatus { get; set; }
    }
}
