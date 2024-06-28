﻿using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public struct RequestSong
    {
        public Song Song { get; }

        public TimeSpan? Position { get; }

        public TimeSpan Duration { get; }

        private RequestSong(Song song, TimeSpan? position, TimeSpan duration) : this()
        {
            Song = song;
            Position = position;
            Duration = duration;
        }

        public override string ToString()
        {
            return $"{Song.FullPath} @ {Position} / {Duration}";
        }

        public static RequestSong? Start(Song? song)
        {
            return song.HasValue ? (RequestSong?)Get(song.Value, TimeSpan.Zero, TimeSpan.Zero) : null;
        }

        public static RequestSong Get(Song song, TimeSpan? position, TimeSpan? duration = null)
        {
            return new RequestSong(song, position, duration ?? TimeSpan.Zero);
        }

        public static RequestSong? Get(Song? song, TimeSpan? position, TimeSpan? duration = null)
        {
            return song.HasValue ? (RequestSong?)Get(song.Value, position, duration) : null;
        }
    }
}
