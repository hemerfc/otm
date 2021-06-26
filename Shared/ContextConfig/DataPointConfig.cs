using System.Collections.Generic;

namespace Otm.Shared.ContextConfig
{
    public class DataPointConfig
    {
        public string Name { get; set; }
        public bool DebugMessages { get; set; } = false;
        public string Script { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }

        public List<DataPointParamConfig> Params { get; set; }
    }
}