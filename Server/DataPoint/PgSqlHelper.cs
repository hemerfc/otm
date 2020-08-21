using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Otm.Server.DataPoint
{
    public static class PgSqlHelper
    {
        private static Dictionary<Type, NpgsqlDbType> typeMap;

        // Create and populate the dictionary in the static constructor
        static PgSqlHelper()
        {
            typeMap = new Dictionary<Type, NpgsqlDbType>
            {
                [typeof(string)] = NpgsqlDbType.Text,
                [typeof(char[])] = NpgsqlDbType.Char,
                [typeof(byte)] = NpgsqlDbType.Smallint,
                [typeof(short)] = NpgsqlDbType.Smallint,
                [typeof(int)] = NpgsqlDbType.Integer,
                [typeof(long)] = NpgsqlDbType.Bigint,
                [typeof(byte[])] = NpgsqlDbType.Bytea,
                [typeof(bool)] = NpgsqlDbType.Boolean,
                [typeof(DateTime)] = NpgsqlDbType.Date,
                [typeof(DateTimeOffset)] = NpgsqlDbType.TimeTz,
                [typeof(decimal)] = NpgsqlDbType.Money,
                [typeof(float)] = NpgsqlDbType.Real,
                [typeof(double)] = NpgsqlDbType.Double,
                [typeof(TimeSpan)] = NpgsqlDbType.Interval
            };
            /* ... and so on ... */
        }

        // Non-generic argument-based method
        public static NpgsqlDbType GetDbType(Type giveType)
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
        public static NpgsqlDbType GetDbType<T>()
        {
            return GetDbType(typeof(T));
        }
    }
}