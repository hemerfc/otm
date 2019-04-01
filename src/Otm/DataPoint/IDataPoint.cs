
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Otm.DataPoint
{
    public interface IDataPoint
    {
        void Execute(IDictionary<string, object> input, Func<IEnumerable<Object>> callback);
    }
}