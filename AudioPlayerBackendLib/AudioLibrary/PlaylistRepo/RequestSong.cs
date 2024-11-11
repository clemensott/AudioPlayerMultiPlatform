using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public struct RequestSong
    {
        public Song Song { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        public bool ContinuePlayback { get; }

        public RequestSong(Song song, TimeSpan position, TimeSpan duration, bool continuePlayback) : this()
        {
            Song = song;
            Position = position;
            Duration = duration;
            ContinuePlayback = continuePlayback;
        }
    }
}
