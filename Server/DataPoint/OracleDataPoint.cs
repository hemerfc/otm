using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Otm.Server.ContextConfig;
using Exception = System.Exception;

namespace Otm.Server.DataPoint
{
    public class OracleDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        public DataPointConfig Config { get; set; }
        public bool DebugMessages { get; set; }
        public string Driver { get; set; }
        public string Script { get; set; }
        public string CronExpression { get; }
        
        public OracleDataPoint(DataPointConfig config)
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
                using (var command = new OracleCommand(Config.Name, conn))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                {
                    
                    command.CommandType = CommandType.StoredProcedure;
                    //command.ArrayBindCount = Config.Params.Count(p => p.Mode == Modes.FromOTM || p.Mode == Modes.Static);
                        
                    foreach (var param in Config.Params)
                    {
                        if (param.Mode == Modes.ToOTM)
                        {
                            //Type type = Type.GetType("System." + Enum.GetName(typeof(TypeCode), param.TypeCode));
                            OracleDbType oracleDbType = ConvertToOracleDbType(param.TypeCode);
                            var oracleParam = command.Parameters.Add(param.Name, oracleDbType);
                            oracleParam.Direction = ParameterDirection.Output;

                            //if (param.TypeCode == TypeCode.String)
                             //   oracleParam.Size = param.Length ?? 0;
                        }
                        else if (param.Mode == Modes.FromOTM)
                        {
                            var pName = param.Name;
                            var pType = ConvertToOracleDbType(param.TypeCode);
                            var pSize = 0;//param.Length ?? 1;
                            var pValue = input[param.Name];
                            var pDir = ParameterDirection.Input;
                            
                            // string name, OracleDbType dbType,int size, object val, ParameterDirection dir
                            var oracleParam = command.Parameters.Add(pName, pType, pSize, pValue, pDir);
                            //var oracleParam = command.Parameters.Add(param.Name, input[param.Name]);
                            oracleParam.Direction = ParameterDirection.Input;
                        }
                        else if (param.Mode == Modes.Static)
                        {
                            var oracleParam = command.Parameters.Add(param.Name, param.Value);
                            oracleParam.Direction = ParameterDirection.Input;
                        }
                    }

                    command.ExecuteNonQuery();

                    foreach (var param in Config.Params)
                    {
                        if (param.Mode == Modes.ToOTM)
                        {
                            var dbValue = UnBoxOracleType(command.Parameters[param.Name].Value);
                            output[param.Name] = CastToParamTypeCode(dbValue, param.TypeCode);
                        }
                    }

                    return output;
                }
            }
        }

        private object CastToParamTypeCode(object value, TypeCode typeCode)
        {
            if (value == null)
                return null;

            return typeCode switch
            {
                TypeCode.String => value.ToString(),
                TypeCode.Int16 => Convert.ToInt16(value),
                TypeCode.Int32 => Convert.ToInt32(value),
                TypeCode.Int64 => Convert.ToInt64(value),
                TypeCode.DateTime => Convert.ToDateTime(value),
                TypeCode.Decimal => Convert.ToDecimal(value),
                TypeCode.Boolean => Convert.ToBoolean(value),
                TypeCode.Double => Convert.ToDouble(value),
                TypeCode.Byte => Convert.ToByte(value),
                TypeCode.Char => Convert.ToChar(value),
                TypeCode.Single => Convert.ToSingle(value),
                _ => throw new Exception($"Type not supported! OracleDataPoint Type: {typeCode}")
            };
        }


        /// <summary>
        /// The need for this method is highly annoying.
        /// When Oracle sets its output parameters, the OracleParameter.Value property
        /// is set to an internal Oracle type, not its equivelant System type.
        /// For example, strings are returned as OracleString, DBNull is returned
        /// as OracleNull, blobs are returned as OracleBinary, etc...
        /// So these Oracle types need unboxed back to their normal system types.
        /// </summary>
        /// <param name="oracleType">Oracle type to unbox.</param>
        /// <returns></returns>
        internal static object UnBoxOracleType(object oracleType)
        {
            if (oracleType == null)
                return null;

            Type T = oracleType.GetType();
            if (T == typeof(OracleString))
            {
                if (((OracleString)oracleType).IsNull)
                    return null;
                return ((OracleString)oracleType).Value;
            }
            else if (T == typeof(OracleDecimal))
            {
                if (((OracleDecimal)oracleType).IsNull)
                    return null;
                return ((OracleDecimal)oracleType).Value;
            }
            else if (T == typeof(OracleBinary))
            {
                if (((OracleBinary)oracleType).IsNull)
                    return null;
                return ((OracleBinary)oracleType).Value;
            }
            else if (T == typeof(OracleBlob))
            {
                if (((OracleBlob)oracleType).IsNull)
                    return null;
                return ((OracleBlob)oracleType).Value;
            }
            else if (T == typeof(OracleDate))
            {
                if (((OracleDate)oracleType).IsNull)
                    return null;
                return ((OracleDate)oracleType).Value;
            }
            else if (T == typeof(OracleTimeStamp))
            {
                if (((OracleTimeStamp)oracleType).IsNull)
                    return null;
                return ((OracleTimeStamp)oracleType).Value;
            }
            else // not sure how to handle these.
                return oracleType;
        }        
        
        public static object GetParamValue(OracleParameter param) {
            if(param == null || param.Value==null) {
                return null;
            }

            var oracleType=param.Value;

            if((bool)oracleType.GetType().GetProperty("IsNull").GetValue(oracleType)!) {
                return null;
            }
            return oracleType.GetType().GetProperty("Value")?.GetValue(oracleType);
        }
        
        private OracleConnection CreateConnection()
        {
            var conn = new OracleConnection(Config.Config);
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
        
        private OracleDbType ConvertToOracleDbType(TypeCode typeCode)
        {
            switch (typeCode)
            {
                /*
                   [typeof(string)] = OracleDbType.Varchar2,
                   [typeof(char[])] = OracleDbType.Varchar2,
                   [typeof(byte)] = OracleDbType.Byte,
                   [typeof(short)] = OracleDbType.Int16,
                   [typeof(int)] = OracleDbType.Int32,
                   [typeof(long)] = OracleDbType.Int64,
                   [typeof(byte[])] = OracleDbType.Blob,
                   [typeof(bool)] = OracleDbType.Boolean,
                   [typeof(DateTime)] = OracleDbType.TimeStamp,
                   [typeof(DateTimeOffset)] = OracleDbType.TimeStamp,
                   [typeof(decimal)] = OracleDbType.Decimal,
                   [typeof(float)] = OracleDbType.Decimal,
                   [typeof(double)] = OracleDbType.Decimal,
                   [typeof(TimeSpan)] = OracleDbType.TimeStamp
                 */
                case TypeCode.String: return OracleDbType.Varchar2;
                case TypeCode.Byte: return OracleDbType.Byte;
                case TypeCode.Int16: return OracleDbType.Int16;
                case TypeCode.Int32: return OracleDbType.Int32;
                case TypeCode.Boolean: return OracleDbType.Boolean;
                case TypeCode.DateTime: return OracleDbType.TimeStamp;
                case TypeCode.Decimal: return OracleDbType.Decimal;
                case TypeCode.Single: return OracleDbType.Single;
                case TypeCode.Double: return OracleDbType.Double;
                case TypeCode.Int64: return OracleDbType.Int64;
                case TypeCode.Char: return OracleDbType.Char;
                // Add mappings for other types as needed
                default:
                    throw new Exception($"Type not supported! OracleDataPoint Type: {typeCode}");
            }
        }
        
    }
}

/* Vers�o Utilizando o Sql Client como ex
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NLog;
using Otm.Server.ContextConfig;
using System.Globalization;

namespace Otm.Server.DataPoint
{
    public class OracleDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        private DataPointConfig Config { get; set; }

        private readonly ILogger logger;

        public bool DebugMessages { get; set; }
        public string Driver { get; set; }
        public string Script { get; set; }
        public string CronExpression { get; }


        public OracleDataPoint(DataPointConfig config, ILogger logger)
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
                if (this.DebugMessages)
                    conn.InfoMessage += connection_InfoMessage;

                string cmdString;
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
                using (var command = new OracleCommand(cmdString, conn))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                {
                    if (StoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    if (Config.Params != null)
                    {
                        foreach (var param in Config.Params)
                        {
                            if (param.Mode == Modes.ToOTM)
                            {
                                OracleDbType oracleDbType = ConvertToOracleDbType(param.TypeCode);
                                var oracleParam = command.Parameters.Add(param.Name, oracleDbType);
                                oracleParam.Direction = ParameterDirection.Output;

                                if (param.TypeCode == TypeCode.String)
                                    oracleParam.Size = param.Length ?? 0;
                            }
                            else if (param.Mode == Modes.FromOTM)
                            {
                                var oracleParam = command.Parameters.Add(param.Name, input[param.Name]);
                                oracleParam.Direction = ParameterDirection.Input;
                            }
                            else if (param.Mode == Modes.Static)
                            {
                                var oracleParam = command.Parameters.Add(param.Name, param.Value);
                                oracleParam.Direction = ParameterDirection.Input;
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

        public void connection_InfoMessage(object sender, OracleInfoMessageEventArgs e)
        {
            // This gets the print statements or other messages from Oracle.
            var outputFromStoredProcedure = e.Message;
            logger.Info($"ProcOutput: {outputFromStoredProcedure}");
        }

        private OracleConnection CreateConnection()
        {
            var conn = new OracleConnection(Config.Config);
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
                @"SELECT obj.name AS procedure_name, SUBSTR(par.parameters, 0, LENGTH(par.parameters)) AS parameters
                    FROM all_objects obj
                    JOIN all_source mod ON mod.name = obj.name
                    CROSS APPLY (SELECT p.name || ' ' || p.type_name || ', ' 
                                 FROM all_arguments p
                                 WHERE p.object_id = obj.object_id
                                 ORDER BY p.position
                                 FOR XML PATH ('') ) par (parameters)
                    WHERE obj.object_type IN ('PROCEDURE', 'PACKAGE', 'FUNCTION');";

            var reader = cmd.ExecuteReader();
            var funcs = new List<DataPointFunction>();
            while (reader.HasRows)
            {
                
                var func = new DataPointFunction
                {
                    Name = reader.GetString(0),
                    Parans = reader.GetString(1)
                    .Split(',')
                    .Select(x => x.Trim().Split(' '))
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
                    return TypeCode.String;
                case "date":
                    return TypeCode.DateTime;
                default:
                    throw new Exception($"Type not supported! OracleDataPoint Type: {typeName}");
            }
        }

        private OracleDbType ConvertToOracleDbType(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    return OracleDbType.Int32;
                case TypeCode.String:
                    return OracleDbType.Varchar2;
                case TypeCode.DateTime:
                    return OracleDbType.Date;
                // Add mappings for other types as needed
                default:
                    throw new Exception($"Type not supported! OracleDataPoint Type: {typeCode}");
            }
        }

    }
}
 */
