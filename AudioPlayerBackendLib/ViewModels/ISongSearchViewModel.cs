using AudioPlayerBackend.Audio;
using System.Collections.Generic;
using System.ComponentModel;

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
    }
}
