using System;
using System.Collections.Generic;

namespace AudioPlayerBackendLib
{
    public interface IAudioExtended : IAudio, IDisposable
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
