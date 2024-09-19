using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System;

namespace AudioPlayerBackend.Player
{
    public class MediaOpenedEventArgs : EventArgs
    {
        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        public Song Source { get; }

        public MediaOpenedEventArgs(TimeSpan position, TimeSpan duration, Song source)
        {
            Position = position;
            Duration = duration;
            Source = source;
        }
    }
}
