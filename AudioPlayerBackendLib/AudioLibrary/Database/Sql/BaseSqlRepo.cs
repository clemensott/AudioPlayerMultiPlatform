using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.Database.Sql
{
    public abstract class BaseSqlRepo : IAudioService
    {
        protected ISqlExecuteService sqlExecuteService;

        internal BaseSqlRepo(ISqlExecuteService sqlExecuteService)
        {
            this.sqlExecuteService = sqlExecuteService;
        }

        protected KeyValuePair<string, object> CreateParam(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        protected KeyValuePair<string, object>[] CreateParams(string key1, object value1)
        {
            return new KeyValuePair<string, object>[]
            {
                CreateParam(key1, value1),
            };
        }

        protected KeyValuePair<string, object>[] CreateParams(string key1, object value1, string key2, object value2)
        {
            return new KeyValuePair<string, object>[]
            {
                CreateParam(key1, value1),
                CreateParam(key2, value2),
            };
        }

        protected KeyValuePair<string, object>[] CreateParams(string key1, object value1, 
            string key2, object value2, string key3, object value3)
        {
            return new KeyValuePair<string, object>[]
            {
                CreateParam(key1, value1),
                CreateParam(key2, value2),
                CreateParam(key3, value3),
            };
        }

        public static long? ToNullableLong(bool? value)
        {
            if (value.HasValue)
            {
                return value.Value ? 1L : 0L;
            }

            return null;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            sqlExecuteService.Dispose();
            return Task.CompletedTask;
        }
    }
}
