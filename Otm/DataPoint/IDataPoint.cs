
using System;
using System.Collections.Generic;
using Otm.ContextConfig;

namespace Otm.DataPoint
{
    public interface IDataPoint
    {
        string Name { get; }
        IDictionary<string, object> Execute(IDictionary<string, object> values);
        DataPointParamConfig GetParamConfig(string name);
        bool CheckConnection();
        bool CheckFunction();

        List<DataPointFunction> GetFunctions();
    }
}