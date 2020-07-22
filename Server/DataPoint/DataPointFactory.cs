using System;
using System.Linq;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;

namespace Otm.Server.DataPoint
{
    public static class DataPointFactory
    {
        public static IDictionary<string, IDataPoint> CreateDataPoints(IEnumerable<DataPointConfig> dataPointsConfig, ILogger logger)
        {
            var datapoints = new Dictionary<string, IDataPoint>();

            if (dataPointsConfig != null)
                foreach (var dpConfig in dataPointsConfig)
                {
                    var dataPoint = CreateDataPoint(dpConfig, logger);
                    datapoints.Add(dpConfig.Name, dataPoint);
                }

            return datapoints;
        }

        private static IDataPoint CreateDataPoint(DataPointConfig dpConfig, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(dpConfig.Name))
            {
                var ex = new Exception("Invalid DataPoint name in config. Name:" + dpConfig.Name);
                ex.Data.Add("field", "Name");
                throw ex;
            }

            var staticParam = dpConfig.Params.Where(x => x.Mode == Modes.Static);

            IDataPoint datapoint;

            logger.LogInformation($"DataPoint {dpConfig.Name}: Creating");
            switch (dpConfig.Driver)
            {
                case "pg":
                    datapoint = new PgDataPoint(dpConfig);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
                    break;
                case "mssql":
                    datapoint = new MsSqlDataPoint(dpConfig);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
                    break;
                case "script":
                    datapoint = new ScriptDataPoint(dpConfig);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
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