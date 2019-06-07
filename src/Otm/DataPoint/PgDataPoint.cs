
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Otm.Config;

namespace Otm.DataPoint
{
    public class PgDataPoint : IDataPoint
    {
        public DataPointConfig Config { get; set; }

        public PgDataPoint(DataPointConfig config)
        {
            Config = config;
        }

        public IDictionary<string, object> Execute(IDictionary<string, object> input)
        {
            var output = new Dictionary<string, object>();

            using(var conn = CreateConnection())
            {
                var lparam = Array.ConvertAll(Config.Params, x => $"{x.Name} => @{x.Name}");
                var strParams = string.Join(",", lparam);

                using (var command = conn.CreateCommand())
                {
                    var sql = $"CALL {Config.Name}({strParams})";
                    command.CommandText = sql;

                    foreach(var param in Config.Params)
                    {                        
                        var sqlParam = command.Parameters.AddWithValue(param.Name, input[param.Name]);

                        if (param.Mode == "out")
                        {
                            sqlParam.Direction = ParameterDirection.InputOutput;
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