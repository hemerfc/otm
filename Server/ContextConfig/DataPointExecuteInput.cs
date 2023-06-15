using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public class DataPointExecuteInput
    {
        public string Name { get; set; }
        public List<DataPointExecuteParamsInput> Params { get; set; }

    }
}
