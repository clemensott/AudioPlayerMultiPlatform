using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary
{
    public interface ILibraryRepo
    {
        Task<Library> GetLibrary();

        Task SendPlayStateChange(PlaybackState playState);
        event EventHandler<AudioLibraryChange<PlaybackState>> OnPlayStateChange;

        Task SendVolumeChange(double volume);
        event EventHandler<AudioLibraryChange<double>> OnVolumeChange;

        Task SendPlaylistsChange(IList<PlaylistInfo> playlists);
        event EventHandler<AudioLibraryChange<IList<PlaylistInfo>>> OnPlaylistsChange;

        Task SendSourcePlaylistsChange(IList<SourcePlaylistInfo> sourcePlaylists);
        event EventHandler<AudioLibraryChange<IList<SourcePlaylistInfo>>> OnSourcePlaylistsChange;

        Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots);
        event EventHandler<AudioLibraryChange<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;
    }
}
