using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.Logger;

namespace Otm.DataPoint
{
    public class DataPointFactory : IDataPointFactory
    {
        public IDictionary<string, IDataPoint> CreateDataPoints(IEnumerable<DataPointConfig> dataPointsConfig, ILoggerFactory loggerFactory)
        {
            var datapoints = new Dictionary<string, IDataPoint>();

            foreach (var dpConfig in dataPointsConfig)
            {
                if (string.IsNullOrWhiteSpace(dpConfig.Name))
                {
                    var ex = new Exception("Invalid DataPoint name in config. Name:" + dpConfig.Name);
                    ex.Data.Add("field", "Name");
                    throw ex;
                }
            
                switch (dpConfig.Driver)
                {
                    case "pg":
                        datapoints.Add(dpConfig.Name, new PgDataPoint(dpConfig, loggerFactory));
                        break;
                    default:
                        var ex = new Exception("Invalid DataPointDriver in config. Driver:" + dpConfig.Driver);
                        ex.Data.Add("field", "Driver");
                        throw ex;
                }
            }

            return datapoints;
        }
    }
}