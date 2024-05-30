using AudioPlayerBackend.AudioLibrary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build
{
    public class AudioServices : IAudioService
    {
        public ILibraryRepo Library { get; }

        public IPlaylistsRepo Playlists { get; }

        public IEnumerable<IAudioService> Services { get; }

        public AudioServices(ILibraryRepo library, IPlaylistsRepo playlists, IEnumerable<IAudioService> services)
        {
            Library = library;
            Playlists = playlists;
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
