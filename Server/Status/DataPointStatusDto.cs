using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.Status
{
    public class DataPointStatusDto
    {
        public string Name { get; set; }
        public bool DebugMessages { get; set; }
        public string Script { get; set; }
        public string Driver { get; set; }
    }
}
