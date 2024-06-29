using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class Library
    {
        public PlaybackState PlayState { get; }

        public double Volume { get; }

        public Guid? CurrentPlaylistId { get; }

        public ICollection<PlaylistInfo> Playlists { get; }
    }
}
