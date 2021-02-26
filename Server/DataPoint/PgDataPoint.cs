
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using Otm.Server.ContextConfig;
using Otm.Shared.ContextConfig;

namespace Otm.Server.DataPoint
{
    public class PgDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        public DataPointConfig Config { get; set; }
        public bool DebugMessages { get; set; }
        public string Driver { get;  set; }
        public string Script { get; set; }

        //private static readonly ILogger Logger = LoggerFactory.GetCurrentClassLogger();

        public PgDataPoint(DataPointConfig config)
        {
            Config = config;
            this.DebugMessages = config.DebugMessages;
            this.Driver = config.Driver;
            this.Script = config.Script;
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
                            Type type = Type.GetType("System." + Enum.GetName(typeof(TypeCode), param.TypeCode));
                            var dbType = PgSqlHelper.GetDbType(type);
                            var sqlParam = command.Parameters.Add(param.Name, dbType);
                            sqlParam.Direction = ParameterDirection.Output;

                            if (param.TypeCode == TypeCode.String)
                                sqlParam.Size = param.Length ?? 0;
                        }
                        else if (param.Mode == Modes.FromOTM)
                        {
                            var sqlParam = command.Parameters.AddWithValue(param.Name, input[param.Name]);
                            sqlParam.Direction = ParameterDirection.Input;
                        }
                        else if (param.Mode == Modes.Static)
                        {
                            var sqlParam = command.Parameters.AddWithValue(param.Name, param.Value);
                            sqlParam.Direction = ParameterDirection.Input;
                        }
                    }

                    command.ExecuteNonQuery();

                    foreach (var param in Config.Params)
                    {
                        if (param.Mode == Modes.ToOTM)
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