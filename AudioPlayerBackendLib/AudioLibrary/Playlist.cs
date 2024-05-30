using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioPlayerBackend.AudioLibrary
{
    public class Playlist
    {
     public   Guid Id { get; }

        public string Name { get; }

        public OrderType Shuffle { get; }

        public LoopType Loop { get; }

        public double PlaybackRate { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        public RequestSong RequestSong { get; }

        public Guid CurrentSongId { get; }

        public IList<Song> Songs { get; }
    }
}
