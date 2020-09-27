using StdOttStandard.Linq.DataStructures.Observable;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioService : IAudioServiceBase, INotifyPropertyChanged
    {
        bool IsSearching { get; }

        new IPlaylist CurrentPlaylist { get; set; }

        new ObservableCollection<ISourcePlaylist> SourcePlaylists { get; }

        new ObservableCollection<IPlaylist> Playlists { get; }

        IEnumerable<Song> AllSongs { get; }

        IEnumerable<Song> SearchSongs { get; }

        void SetNextSong();

        void SetPreviousSong();

        void Continue();
    }
}
