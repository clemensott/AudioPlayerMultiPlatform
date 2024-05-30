using AudioPlayerBackend.AudioLibrary;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.ViewModels
{
    public interface ILibraryViewModel : INotifyPropertyChanged
    {
        bool IsLoaded { get; }

        IPlaylistViewModel CurrentPlaylist { get; }

        IList<PlaylistInfo> Playlists { get; }

        IList<SourcePlaylistInfo> SourcePlaylists { get; }
    }
}
