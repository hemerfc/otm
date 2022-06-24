
using System;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Otm.Shared.ContextConfig;

namespace Otm.Server.DataPoint
{
    public interface IDataPoint
    {
        string Name { get; }
        public bool DebugMessages { get; set; }
        public string Script { get; }
        public string Driver { get; }
        public string CronExpression { get; }

        IDictionary<string, object> Execute(IDictionary<string, object> values);
        DataPointParamConfig GetParamConfig(string name);
        bool CheckConnection();
        bool CheckFunction();

        List<DataPointFunction> GetFunctions();
    }
}