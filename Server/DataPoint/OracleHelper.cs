using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace Otm.Server.DataPoint
{
    public static class OracleHelper
    {
        private static Dictionary<Type, OracleDbType> typeMap;

        // Create and populate the dictionary in the static constructor
        static OracleHelper()
        {
            typeMap = new Dictionary<Type, OracleDbType>
            {
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
            };
            /* ... and so on ... */
        }

        // Non-generic argument-based method
        public static OracleDbType GetDbType(Type giveType)
        {
            // Allow nullable types to be handled
            giveType = Nullable.GetUnderlyingType(giveType) ?? giveType;

            if (typeMap.ContainsKey(giveType))
            {
                return typeMap[giveType];
            }

            throw new ArgumentException($"{giveType.FullName} is not a supported .NET class");
        }

        // Generic version
        public static OracleDbType GetDbType<T>()
        {
            return GetDbType(typeof(T));
        }
    }
}