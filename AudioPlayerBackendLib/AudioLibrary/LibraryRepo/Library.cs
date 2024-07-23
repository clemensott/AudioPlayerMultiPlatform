using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class Library
    {
        public PlaybackState PlayState { get; }

        public double Volume { get; }

        public Guid? CurrentPlaylistId { get; }

        public ICollection<PlaylistInfo> Playlists { get; }

        public PlaylistInfo GetCurrentPlaylist()
        {
            if (!CurrentPlaylistId.HasValue) return null;

            return Playlists.First(p => p.Id == CurrentPlaylistId);
        }
    }
}
