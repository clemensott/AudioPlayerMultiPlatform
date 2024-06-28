using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public interface ILibraryRepo
    {
        Task<Library> GetLibrary();

        Task SendPlayStateChange(PlaybackState playState);
        event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;

        Task SendVolumeChange(double volume);
        event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;

        Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId);
        event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;

        //Task SendPlaylistsChange(IList<PlaylistInfo> playlists);
        //event EventHandler<AudioLibraryChange<IList<PlaylistInfo>>> OnPlaylistsChange;

        Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots);
        event EventHandler<AudioLibraryChangeArgs<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;
    }
}
