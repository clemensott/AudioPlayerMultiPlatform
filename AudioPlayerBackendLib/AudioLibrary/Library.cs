using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary
{
    public class Library
    {
        public PlaybackState PlayState { get; set; }

        public double Volume { get; set; }

        public IList<PlaylistInfo> Playlists { get; }

        public IList<SourcePlaylistInfo> SourcePlaylists { get; }

        public IList<FileMediaSourceRoot> fileMediaSourceRoots { get; }
    }
}
