using System;

namespace AudioPlayerBackend.Audio
{
    public struct RequestSong
    {
        public Song Song { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        private RequestSong(Song song, TimeSpan position, TimeSpan duration) : this()
        {
            Song = song;
            Position = position;
            Duration = duration;
        }

        public static RequestSong Get(Song song, TimeSpan? position = null, TimeSpan? duration = null)
        {
            return new RequestSong(song, position ?? TimeSpan.Zero, duration ?? TimeSpan.Zero);
        }

        public static RequestSong? Get(Song? song, TimeSpan? position = null, TimeSpan? duration = null)
        {
            return song.HasValue ? (RequestSong?)Get(song.Value, position, duration) : null;
        }
    }
}
