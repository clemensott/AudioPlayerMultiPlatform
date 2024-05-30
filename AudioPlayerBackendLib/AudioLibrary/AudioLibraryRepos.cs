using System;
using System.Collections.Generic;
using System.Text;

namespace AudioPlayerBackend.AudioLibrary
{
    public class AudioLibraryRepos
    {
        public ILibraryRepo Library { get; }

        public IPlaylistsRepo Playlists { get; }
    }
}
