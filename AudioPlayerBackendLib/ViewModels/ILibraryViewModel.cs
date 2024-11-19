using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Player;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public interface ILibraryViewModel : IAudioService, INotifyPropertyChanged
    {
        bool IsLoaded { get; }

        bool IsClient { get; }

        bool IsLocalFileMediaSource { get; }

        bool IsUpdatingPlaylists { get; }

        PlaybackState PlayState { get; set; }

        int CurrentPlaylistIndex { get; set; }

        IPlaylistViewModel CurrentPlaylist { get; }

        ObservableCollection<PlaylistInfo> Playlists { get; }

        ISongSearchViewModel SongSearch { get; }

        double Volume { get; set; }

        Task RemixSongs(Guid playlistId);

        Task RemovePlaylist(Guid playlistId);
    }
}
