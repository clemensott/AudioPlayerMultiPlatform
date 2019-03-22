﻿using System.Collections.Generic;

namespace AudioPlayerBackend.Audio
{
    public interface ISourcePlaylist : ISourcePlaylistBase, IPlaylist
    {
        bool IsSearching { get; }

        Song[] ShuffledSongs { get; }

        IEnumerable<Song> SearchSongs { get; }

        void Reload();
    }
}
