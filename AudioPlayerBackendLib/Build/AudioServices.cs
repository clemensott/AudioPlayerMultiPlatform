using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build
{
    public class AudioServices : IAudioService
    {
        public IServiceProvider ServiceProvider { get; }

        public IEnumerable<IAudioService> Services { get; }

        public AudioServices(IServiceProvider serviceProvider, IEnumerable<IAudioService> services)
        {
            ServiceProvider = serviceProvider;
            Services = services;
        }

        public Task Start()
        {
            return Task.WhenAll(Services.Select(s => s.Start()));
        }

        public Task Stop()
        {
            return Task.WhenAll(Services.Select(s => s.Stop()));
        }

        public Task Dispose()
        {
            return Task.WhenAll(Services.Select(s => s.Dispose()));
        }
    }
}
