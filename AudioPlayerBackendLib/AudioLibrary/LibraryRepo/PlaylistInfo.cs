using System;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class PlaylistInfo
    {
        public Guid Id { get; }

        public PlaylistType Type { get; }

        public string Name { get; }

        public int SongsCount { get; }

        public PlaylistInfo(Guid id, PlaylistType type, string name, int songsCount)
        {
            Id = id;
            Type = type;
            Name = name;
            SongsCount = songsCount;
        }
    }
}
