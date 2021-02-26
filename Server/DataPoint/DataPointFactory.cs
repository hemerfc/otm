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
            // todo datapoint deve ter um nome
            if (string.IsNullOrWhiteSpace(dpConfig.Name))
            {
                var ex = new Exception($"Invalid DataPoint name in config. Name:{dpConfig.Name}");
                ex.Data.Add("field", "Name");
                throw ex;
            }

            if (dpConfig.Params == null || dpConfig.Params.Count <= 0)
            {
                var ex = new Exception($"DataPoint must have at least one parameter. DataPoint:{dpConfig.Name}");
                ex.Data.Add("field", "Params");
                throw ex;
            }

            foreach (var param in dpConfig.Params)
            {
                // se é do tipo string precisa ter o comprimento
                if (param.TypeCode == TypeCode.String && (param.Length ?? 0) == 0)
                {
                    var ex = new Exception($"DataPoint Param of type String must have Lenght. DataPoint:{dpConfig.Name} Param:{param.Name}");
                    ex.Data.Add("field", "Length");
                    throw ex;
                }
            }

            //var staticParam = dpConfig.Params.Where(x => x.Mode == Modes.Static);

            IDataPoint datapoint;

            logger.LogInformation($"DataPoint {dpConfig.Name}: Creating");
            switch (dpConfig.Driver)
            {
                case "pg":
                    datapoint = new PgDataPoint(dpConfig);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
                    break;
                case "mssql":
                    datapoint = new MsSqlDataPoint(dpConfig, logger);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
                    break;
                case "script":
                    datapoint = new ScriptDataPoint(dpConfig);
                    logger.LogInformation($"DataPoint {dpConfig.Name}: Created");
                    break;
                default:
                    var ex = new Exception("Invalid DataPointDriver in config. Driver:" + dpConfig.Driver);
                    ex.Data.Add("field", "Driver");
                    throw ex;
            }

            return datapoint;
        }
    }
}