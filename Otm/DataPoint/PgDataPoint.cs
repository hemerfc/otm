
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NLog;
using Npgsql;
using Otm.ContextConfig;
using Otm.Logger;

namespace Otm.DataPoint
{
    public class PgDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        private DataPointConfig Config { get; set; }
        //private static readonly ILogger Logger = LoggerFactory.GetCurrentClassLogger();

        public PgDataPoint(DataPointConfig config)
        {
            Config = config;
        }

        public DataPointParamConfig GetParamConfig(string name)
        {
            return Config.Params.FirstOrDefault(x => x.Name == name);
        }

        public IDictionary<string, object> Execute(IDictionary<string, object> input)
        {
            var output = new Dictionary<string, object>();

            using (var conn = CreateConnection())
            {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                using (var command = new NpgsqlCommand(Config.Name, conn))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var param in Config.Params)
                    {

                        if (param.Mode == Modes.ToOTM)
                        {
                            var sqlParam = command.Parameters.AddWithValue(param.Name, null);
                            sqlParam.Direction = ParameterDirection.Output;
                        }
                        else if (param.Mode == Modes.FromOTM)
                        {
                            var sqlParam = command.Parameters.AddWithValue(param.Name, input[param.Name]);
                            sqlParam.Direction = ParameterDirection.Input;
                        }
                        else
                        {
                            throw new Exception($"Parameter mode not suported! DataPoint:{Config.Name} Param:{param.Name} Mode:{param.Mode}");
                        }
                    }

                    command.ExecuteNonQuery();

                    foreach (var param in Config.Params)
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

        public bool CheckConnection()
        {
            throw new NotImplementedException();
        }

        public bool CheckFunction()
        {
            throw new NotImplementedException();
        }

        public List<DataPointFunction> GetFunctions()
        {
            throw new NotImplementedException();
        }
    }
}