using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    internal class AutoUpdateLibraryService : IAudioService
    {
        public Task Start()
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }
    }
}
