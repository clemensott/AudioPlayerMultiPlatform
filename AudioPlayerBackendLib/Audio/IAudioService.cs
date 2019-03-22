using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioService : IAudioServiceBase, INotifyPropertyChanged
    {
        new ISourcePlaylist SourcePlaylist { get; }

        new IPlaylist CurrentPlaylist { get; set; }

        new IPlaylist[] Playlists { get; set; }

        void SetNextSong();

        void SetPreviousSong();

        void Continue();

        IEnumerable<IPlaylist> GetAllPlaylists();
    }
}
