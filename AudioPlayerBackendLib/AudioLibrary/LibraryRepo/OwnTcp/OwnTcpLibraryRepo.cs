using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp
{
    internal class OwnTcpLibraryRepo : ILibraryRepo
    {
        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;
        public event EventHandler<AudioLibraryChangeArgs<IList<PlaylistInfo>>> OnPlaylistsChange;

        public Task<Library> GetLibrary()
        {
            throw new NotImplementedException();
        }

        public Task SendPlayStateChange(PlaybackState playState)
        {
            throw new NotImplementedException();
        }

        public Task SendVolumeChange(double volume)
        {
            throw new NotImplementedException();
        }

        public Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            throw new NotImplementedException();
        }

        public Task SendPlaylistsChange(IList<PlaylistInfo> playlists)
        {
            throw new NotImplementedException();
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        public Task Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
