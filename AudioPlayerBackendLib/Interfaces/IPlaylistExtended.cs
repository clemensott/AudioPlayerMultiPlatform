using System.Collections.Generic;

namespace AudioPlayerBackend
{
    public interface IPlaylistExtended : IPlaylist
    {
        bool IsSearching { get; }

        IEnumerable<Song> AllSongs { get; }

        IEnumerable<Song> SearchSongs { get; }
    }
}
