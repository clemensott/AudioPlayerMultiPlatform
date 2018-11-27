using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public interface IAudioExtended : IAudio, IDisposable, INotifyPropertyChanged
    {
        bool IsSearching { get; }

        IEnumerable<Song> AllSongs { get; }

        IEnumerable<Song> SearchSongs { get; }

        IPlayer Player { get; }

        void SetNextSong();

        void SetPreviousSong();

        void Reload();
    }
}
