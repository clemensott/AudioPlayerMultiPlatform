using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.Database.Sql
{
    public interface ISqlExecuteService : IDisposable
    {
        Task<int> ExecuteNonQueryAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters = null);

        Task<T> ExecuteScalarAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters = null);

        Task<object> ExecuteScalarAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters = null);

        Task<T> ExecuteReadFirstAsync<T>(Func<DbDataReader, T> modelConverter, string sql, IEnumerable<KeyValuePair<string, object>> parameters = null);

        Task<IList<T>> ExecuteReadAllAsync<T>(Func<DbDataReader, T> modelConverter, string sql, IEnumerable<KeyValuePair<string, object>> parameters = null);
    }
}
