using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.OwnTcp
{
    internal class OwnTcpLibraryRepo : ILibraryRepo
    {
        public event EventHandler<AudioLibraryChange<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChange<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChange<Guid?>> OnCurrentPlaylistIdChange;
        public event EventHandler<AudioLibraryChange<IList<PlaylistInfo>>> OnPlaylistsChange;
        public event EventHandler<AudioLibraryChange<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;

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

        public Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots)
        {
            throw new NotImplementedException();
        }
    }
}
