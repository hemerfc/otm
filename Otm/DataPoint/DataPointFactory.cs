using System;
using System.Linq;
using System.Collections.Generic;
using NLog;
using Otm.ContextConfig;
using Otm.Logger;

namespace Otm.DataPoint
{
    public class DataPointFactory : IDataPointFactory
    {
        private static ILogger Logger = LoggerFactory.GetCurrentClassLogger();

        public IDictionary<string, IDataPoint> CreateDataPoints(IEnumerable<DataPointConfig> dataPointsConfig)
        {
            var datapoints = new Dictionary<string, IDataPoint>();

            if (dataPointsConfig != null)
                foreach (var dpConfig in dataPointsConfig)
                {
                    var dataPoint = CreateDataPoint(dpConfig);
                    datapoints.Add(dpConfig.Name, dataPoint);
                }

            return datapoints;
        }

        public IDataPoint CreateDataPoint(DataPointConfig dpConfig)
        {
            if (string.IsNullOrWhiteSpace(dpConfig.Name))
            {
                var ex = new Exception("Invalid DataPoint name in config. Name:" + dpConfig.Name);
                ex.Data.Add("field", "Name");
                throw ex;
            }

            var staticParam = dpConfig.Params.Where(x => x.Mode == Modes.Static);

            IDataPoint datapoint;

            switch (dpConfig.Driver)
            {
                case "pg":
                    datapoint = new PgDataPoint(dpConfig);
                    Logger.Error($"DataPoint {dpConfig.Name}: Created");
                    break;
                case "mssql":
                    datapoint = new MsSqlDataPoint(dpConfig);
                    Logger.Error($"DataPoint {dpConfig.Name}: Created");
                    break;
                default:
                    datapoint = null;
                    var ex = new Exception("Invalid DataPointDriver in config. Driver:" + dpConfig.Driver);
                    ex.Data.Add("field", "Driver");
                    throw ex;
            }

            return datapoint;
        }
    }
}