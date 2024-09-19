using System;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class PlaylistInfo
    {
        public Guid Id { get; }

        public PlaylistType Type { get; }

        public string Name { get; }

        public int SongsCount { get; }

        public DateTime? FilesLastUpdated { get; }
        
        public DateTime? SongsLastUpdated { get; }

        public PlaylistInfo(Guid id, PlaylistType type, string name, int songsCount, DateTime? filesLastUpdated, DateTime? songsLastUpdated)
        {
            Id = id;
            Type = type;
            Name = name;
            SongsCount = songsCount;
            FilesLastUpdated = filesLastUpdated;
            SongsLastUpdated = songsLastUpdated;
        }
    }
}
