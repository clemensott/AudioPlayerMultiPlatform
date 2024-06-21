using System;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class PlaylistInfo
    {
        public Guid Id { get; }

        public PlaylistType Type { get; }

        public string Name { get; }

        public int SongsCount { get; }
    }
}
