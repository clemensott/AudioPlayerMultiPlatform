using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Player;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.ViewModels
{
    public interface ILibraryViewModel : IAudioService, INotifyPropertyChanged
    {
        bool IsLoaded { get; }

        bool IsLocalFileMediaSource { get; }

        PlaybackState PlayState { get; set; }

        IPlaylistViewModel CurrentPlaylist { get; }

        IList<PlaylistInfo> Playlists { get; }

        ISongSearchViewModel SongSearuch { get; }

        double Volume { get; set; }
    }
}
