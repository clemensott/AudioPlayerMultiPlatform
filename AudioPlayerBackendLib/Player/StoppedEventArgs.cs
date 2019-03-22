using System;

namespace AudioPlayerBackend.Player
{
  public  class StoppedEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public StoppedEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
