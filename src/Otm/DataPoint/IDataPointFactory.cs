using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;

namespace Otm.DataPoint
{
    public interface IDataPointFactory
    {
        IDictionary<string, IDataPoint> CreateDataPoints(
            IEnumerable<DataPointConfig> dataPointsConfig);
    }
}