using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.Audio
{
    public interface IPlaylist : IPlaylistBase, INotifyPropertyChanged
    {
        IEnumerable<Song> AllSongs { get; }
    }
}
