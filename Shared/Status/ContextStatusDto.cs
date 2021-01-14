using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Shared.Status
{
    public class ContextStatusDto
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public IDictionary<string, DeviceStatusDto> DeviceStatus { get;set;}
}
}
