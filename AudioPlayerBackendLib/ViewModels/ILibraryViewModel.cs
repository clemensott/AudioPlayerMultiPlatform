using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public interface ILibraryViewModel : IAudioService, INotifyPropertyChanged
    {
        bool IsLoaded { get; }

        bool IsLocalFileMediaSource { get; }

        PlaybackState PlayState { get; set; }

        int CurrentPlaylistIndex { get; set; }

        IPlaylistViewModel CurrentPlaylist { get; }

        IList<PlaylistInfo> Playlists { get; }

        ISongSearchViewModel SongSearuch { get; }

        double Volume { get; set; }

        Task RemixSongs(Guid playlistId);

        Task RemovePlaylist(Guid playlistId);
    }
}
