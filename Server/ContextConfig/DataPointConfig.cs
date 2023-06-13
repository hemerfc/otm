using System;
using System.Collections.Generic;

namespace Otm.Server.ContextConfig
{
    public class DataPointConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool DebugMessages { get; set; } = false;
        public string Script { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }
        public string CronExpression { get; set; }
        public string Tipo { get; set; }
        public List<DataPointParamConfig> Params { get; set; }
        public string ContextName { get; set; }
        public string tipoTempo { get; set; }
    }
}