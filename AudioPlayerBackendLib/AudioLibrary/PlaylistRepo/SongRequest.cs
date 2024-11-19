using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public struct SongRequest
    {
        public Guid Id { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        /// <summary>
        /// If true signials player that it should continue the playback without any changes if it matches the playing song
        /// </summary>
        public bool ContinuePlayback { get; }

        private SongRequest(Guid id, TimeSpan position, TimeSpan duration, bool continuePlayback) : this()
        {
            Id = id;
            Position = position;
            Duration = duration;
            ContinuePlayback = continuePlayback;
        }

        public SongRequest CloneWithContinuePlayback()
        {
            return new SongRequest(Id, Position, Duration, true);
        }

        public override string ToString()
        {
            return $"{Id} @ {Position} / {Duration}";
        }

        public static SongRequest? Start(Guid? id)
        {
            return id.HasValue ? (SongRequest?)Get(id.Value, TimeSpan.Zero, TimeSpan.Zero) : null;
        }

        public static SongRequest Get(Guid id, TimeSpan? position, TimeSpan? duration = null, bool continuePlayback = false)
        {
            return new SongRequest(id, position ?? TimeSpan.Zero, duration ?? TimeSpan.Zero, continuePlayback);
        }

        public static SongRequest? Get(Guid? id, TimeSpan? position, TimeSpan? duration = null, bool continuePlayback = false)
        {
            return id.HasValue ? (SongRequest?)Get(id.Value, position, duration, continuePlayback) : null;
        }
    }
}
