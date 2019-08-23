
using System;
using System.Collections.Generic;
using Otm.Config;

namespace Otm.DataPoint
{
    public interface IDataPoint
    {
        IDictionary<string, object> Execute(IDictionary<string, object> values);
        DataPointParamConfig GetParamConfig(string name);
    }
}