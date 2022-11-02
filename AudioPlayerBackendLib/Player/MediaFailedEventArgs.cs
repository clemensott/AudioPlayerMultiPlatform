using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public class MediaFailedEventArgs : EventArgs
    {
        public Song? Song { get; }

        public Exception Exception { get; }

        public MediaFailedEventArgs(Song? song, Exception exception)
        {
            Song = song;
            Exception = exception;
        }
    }
}
