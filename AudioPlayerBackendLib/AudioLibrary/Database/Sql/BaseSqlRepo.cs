using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.Database.Sql
{
    public abstract class BaseRepo : IAudioService
    {
        protected ISqlExecuteService sqlExecuteService;

        internal BaseRepo(ISqlExecuteService sqlExecuteService)
        {
            this.sqlExecuteService = sqlExecuteService;
        }

        protected KeyValuePair<string, object> CreateParam(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
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
