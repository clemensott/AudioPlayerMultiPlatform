using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public interface ISongSearchViewModel : INotifyPropertyChanged
    {
        bool IsEnabled { get; }

        bool IsSearching { get; }

        bool IsSearchShuffle { get; set; }

        string SearchKey { get; set; }

        IEnumerable<Song> SearchSongs { get; }

        void Enable();

        void Disable();

        void Dispose();

        Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType);
    }
}
