
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Otm.DataPoint
{
    public interface IDataPoint
    {
        IDictionary<string, object> Execute(IDictionary<string, object> values);
    }
}