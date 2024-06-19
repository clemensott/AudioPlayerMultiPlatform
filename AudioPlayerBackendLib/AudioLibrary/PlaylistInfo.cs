using System;

namespace AudioPlayerBackend.AudioLibrary
{
    public class PlaylistInfo
    {
        public Guid Id { get; }

        public PlaylistType Type { get; }

        public string Name { get; }

        public int SongsCount { get; }
    }
}
