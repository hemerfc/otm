
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Otm.Server.ContextConfig;
using System.Data.SqlClient;
using Otm.Shared.ContextConfig;
using NLog;

namespace Otm.Server.DataPoint
{
    public class MsSqlDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        private DataPointConfig Config { get; set; }

        private readonly ILogger logger;

        public bool DebugMessages { get; set; }
        public string Driver { get; set; }
        public string Script { get; set; }
        public string CronExpression { get; }


        public MsSqlDataPoint(DataPointConfig config, ILogger logger)
        {
            Config = config;
            this.logger = logger;
            this.DebugMessages = config.DebugMessages;
            this.Driver = config.Driver;
            this.Script = config.Script;
            this.CronExpression = config.CronExpression;
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
                if(this.DebugMessages)
                    conn.InfoMessage += connection_InfoMessage;

                String cmdString;
                bool StoredProcedure = true;

                if (!string.IsNullOrWhiteSpace(this.Script))
                {
                    cmdString = this.Script;
                    StoredProcedure = false;
                }
                else
                {
                    cmdString = Config.Name;
                }

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                using (var command = new SqlCommand(cmdString, conn))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                {
                    if (StoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    if (Config.Params != null) { 
                        foreach (var param in Config.Params)
                        {
                            if (param.Mode == Modes.ToOTM)
                            {
                                Type type = Type.GetType("System." + Enum.GetName(typeof(TypeCode), param.TypeCode));
                                var dbType = MsSqlHelper.GetDbType(type);
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
                    }

                    

                    command.ExecuteNonQuery();

                    if (Config.Params != null)
                    {
                        foreach (var param in Config.Params)
                        {
                            if (param.Mode == Modes.ToOTM)
                                output[param.Name] = command.Parameters[param.Name].Value;
                        }
                    }



                    return output;
                }
            }
        }

        public void connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            // this gets the print statements (maybe the error statements?)
            var outputFromStoredProcedure = e.Message;
            logger.Info($"ProcOutput: {outputFromStoredProcedure}");
        }

        private SqlConnection CreateConnection()
        {
            var conn = new SqlConnection(Config.Config);
            conn.Open();
            return conn;
        }

        public bool CheckFunction()
        {
            var funcs = GetFunctions();

            var func = funcs.FirstOrDefault(f => f.Name == Config.Name);

            if (func != null)
            {
                var names1 = Config.Params.Select(x => x.Name);
                var names2 = func.Parans.Select(x => x.Key);

                return names1.All(names2.Contains) && names1.Count() == names2.Count();
            }

            return false;
        }

        public bool CheckConnection()
        {
            using (var conn = CreateConnection())
            {
                return true;
            }
        }

        public List<DataPointFunction> GetFunctions()
        {
            using var conn = CreateConnection();

            var cmd = conn.CreateCommand();

            cmd.CommandText =
                @"select obj.name as procedure_name, substring(par.parameters, 0, len(par.parameters)) as parameters
                    from sys.objects obj
                    join sys.sql_modules mod on mod.object_id = obj.object_id
                    cross apply (select p.name + ' ' + TYPE_NAME(p.user_type_id) + ', ' 
                                 from sys.parameters p
                                 where p.object_id = obj.object_id and p.parameter_id != 0 
                                 for xml path ('') ) par (parameters)
                    where obj.type in ('P', 'X');";

            var reader = cmd.ExecuteReader();
            var funcs = new List<DataPointFunction>();
            while (reader.HasRows)
            {
                var func = new DataPointFunction
                {
                    Name = reader.GetString(0),
                    Parans = reader.GetString(1)
                                    .Split(',')
                                    .Select(x => x.Split(' '))
                                    .Select(x => (Name: x[0], Type: GetTypeCode(x[1])))
                                    .ToDictionary(x => x.Name, x => x.Type)
                };

                funcs.Add(func);
            }

            return funcs;
        }

        private TypeCode GetTypeCode(string typeName)
        {
            switch (typeName)
            {
                case "int":
                    return TypeCode.Int32;
                case "varchar":
                case "nvarchar":
                    return TypeCode.String;
                default:
                    throw new Exception($"Type not suported! MsSqlDataPoint Type: {typeName}");
            }
        }
    }
}