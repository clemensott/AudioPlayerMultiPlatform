using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
  public  class PlaybackStoppedEventArgs : EventArgs
    {
        public Song? Song { get; }

        public Exception Exception { get;  }

        public PlaybackStoppedEventArgs(Song? song)
        {
            Song = song;
        }

        public PlaybackStoppedEventArgs(Song? song, Exception exception)
        {
            Song = song;
            Exception = exception;
        }
    }
}
