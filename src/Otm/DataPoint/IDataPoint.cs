
using System;
using System.Collections.Generic;

namespace Otm.DataPoint
{
    public interface IDataPoint
    {
        IDictionary<string, object> Execute(IDictionary<string, object> values);
    }
}