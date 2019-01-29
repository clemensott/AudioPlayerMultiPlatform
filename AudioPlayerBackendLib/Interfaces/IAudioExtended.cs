using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public interface IAudioExtended : IAudio, IDisposable, INotifyPropertyChanged
    {
        IPlayer Player { get; }

        void SetNextSong();

        void SetPreviousSong();

        void Reload();

        IEnumerable<IPlaylistExtended> GetAllPlaylists();

        IPlaylistExtended GetPlaylist(Guid id);
    }
}
