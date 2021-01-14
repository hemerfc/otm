using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Shared.Status
{
    public class OtmStatusDto
    {
        public Dictionary<string, ContextStatusDto> OtmContexts { get; set; }
    }
}
