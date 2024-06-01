using AudioPlayerBackend.AudioLibrary;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.ViewModels
{
    public interface ILibraryViewModel : IAudioService, INotifyPropertyChanged
    {
        bool IsLoaded { get; }

        IPlaylistViewModel CurrentPlaylist { get; set; }

        IList<PlaylistInfo> Playlists { get; }

        ISongSearchViewModel SongSearuch { get; }

        double Volume { get; set; }
    }
}
