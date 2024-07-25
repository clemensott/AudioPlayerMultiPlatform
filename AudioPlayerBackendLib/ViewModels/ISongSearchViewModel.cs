using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public interface ISongSearchViewModel : IAudioService, INotifyPropertyChanged
    {
        bool IsEnabled { get; }

        bool IsSearching { get; }

        bool IsSearchShuffle { get; set; }

        string SearchKey { get; set; }

        IEnumerable<Song> SearchSongs { get; }

        Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType);
    }
}
