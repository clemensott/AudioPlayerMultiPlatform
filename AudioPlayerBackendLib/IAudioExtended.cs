using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackendLib
{
    public interface IAudioExtended : IAudio, IDisposable, INotifyPropertyChanged
    {
        bool IsSearching { get; }

        IEnumerable<Song> AllSongs { get; }

        IEnumerable<Song> SearchSongs { get; }

        IntPtr? WindowHandle { get; }

        void SetNextSong();

        void SetPreviousSong();

        void Reload();
    }
}
