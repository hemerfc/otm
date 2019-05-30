
using System;
using System.Collections.Generic;
using Otm.Config;

namespace Otm.DataPoint
{
    public class OdbcDataPoint : IDataPoint
    {
        public OdbcDataPoint(DataPointConfig config)
        {
            
        }

        public void Execute(IDictionary<string, object> input, Func<IEnumerable<Object>> callback)
        {
            throw new NotImplementedException();
        }
    }
}