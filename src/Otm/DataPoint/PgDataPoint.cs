
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using Otm.Config;
using Otm.Logger;

namespace Otm.DataPoint
{
    public class PgDataPoint : IDataPoint
    {
        private DataPointConfig Config { get; set; }
        private ILoggerFactory LoggerFactory { get; set; }

        public PgDataPoint(DataPointConfig config, ILoggerFactory loggerFactory)
        {
            Config = config;
            LoggerFactory = loggerFactory;
        }

        public DataPointParamConfig GetParamConfig(string name)
        {
            return Config.Params.FirstOrDefault(x => x.Name == name);
        }

        public IDictionary<string, object> Execute(IDictionary<string, object> input)
        {
            var output = new Dictionary<string, object>();

            using(var conn = CreateConnection())
            {

                using (var command = new NpgsqlCommand(Config.Name, conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach(var param in Config.Params)
                    {                        
                        var sqlParam = command.Parameters.AddWithValue(param.Name, input[param.Name]);

                        if (param.Mode == "out")
                        {
                            sqlParam.Direction = ParameterDirection.Output;
                        } 
                        else if (param.Mode == "in") 
                        {
                            sqlParam.Direction = ParameterDirection.Input;
                        }
                        else 
                        {
                            throw new Exception($"Parameter mode not suported! DataPoint:{Config.Name} Param:{param.Name} Mode:{param.Mode}");
                        }
                    }

                    command.ExecuteNonQuery();

                    foreach(var param in Config.Params)
                    {
                        output[param.Name] = command.Parameters[param.Name].Value;
                    }

                    return output;
                }
            }
        }

        private NpgsqlConnection CreateConnection()
        {
            var conn = new NpgsqlConnection(Config.Config);
            conn.Open();
            return conn;
        }
    }
}