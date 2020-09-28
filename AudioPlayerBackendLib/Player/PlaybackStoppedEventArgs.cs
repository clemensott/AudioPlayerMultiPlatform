using System;

namespace AudioPlayerBackend.Player
{
  public  class PlaybackStoppedEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public PlaybackStoppedEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
