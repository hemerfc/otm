using System.Collections.Generic;

namespace Otm.ContextConfig
{
    public class DataPointConfig
    {
        public string Name { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }

        public List<DataPointParamConfig> Params { get; set; }
    }
}