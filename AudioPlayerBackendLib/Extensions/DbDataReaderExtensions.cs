using System;
using System.Data.Common;

namespace AudioPlayerBackend.Extensions
{
    static class DbDataReaderExtensions
    {
        public static bool? GetBooleanFromNullableLong(this DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetInt64(ordinal) == 1L;
        }

        public static long GetInt64(this DbDataReader reader, string name)
        {
            return reader.GetInt64(reader.GetOrdinal(name));
        }

        public static long? GetInt64Nullable(this DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetInt64(reader.GetOrdinal(name));
        }

        public static double GetDouble(this DbDataReader reader, string name)
        {
            return reader.GetDouble(reader.GetOrdinal(name));
        }

        public static string GetString(this DbDataReader reader, string name)
        {
            return reader.GetString(reader.GetOrdinal(name));
        }

        public static string GetStringNullable(this DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return reader.GetString(ordinal);
        }

        public static Guid GetGuidFromString(this DbDataReader reader, string name)
        {
            return Guid.Parse(GetString(reader, name));
        }

        public static Guid? GetGuidNullableFromString(this DbDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Guid.Parse(reader.GetString(ordinal));
        }

        public static TimeSpan GetTimespanFromInt64(this DbDataReader reader, string name)
        {
            return new TimeSpan(reader.GetInt64(name));
        }
    }
}
