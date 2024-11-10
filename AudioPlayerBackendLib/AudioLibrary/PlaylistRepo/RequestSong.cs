using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public struct RequestSong
    {
        public Song Song { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        /// <summary>
        /// If true signials player that it should continue the playback without any changes if it matches the playing song
        /// </summary>
        public bool ContinuePlayback { get; }

        private RequestSong(Song song, TimeSpan position, TimeSpan duration, bool continuePlayback) : this()
        {
            Song = song;
            Position = position;
            Duration = duration;
            ContinuePlayback = continuePlayback;
        }

        public override string ToString()
        {
            return $"{Song.FullPath} @ {Position} / {Duration}";
        }

        public static RequestSong? Start(Song? song)
        {
            return song.HasValue ? (RequestSong?)Get(song.Value, TimeSpan.Zero, TimeSpan.Zero) : null;
        }

        public static RequestSong Get(Song song, TimeSpan? position, TimeSpan? duration = null, bool continuePlayback = false)
        {
            return new RequestSong(song, position ?? TimeSpan.Zero, duration ?? TimeSpan.Zero, continuePlayback);
        }

        public static RequestSong? Get(Song? song, TimeSpan? position, TimeSpan? duration = null, bool continuePlayback = false)
        {
            return song.HasValue ? (RequestSong?)Get(song.Value, position, duration, continuePlayback) : null;
        }
    }
}
