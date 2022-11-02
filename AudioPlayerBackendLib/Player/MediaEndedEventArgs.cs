using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public class MediaEndedEventArgs : EventArgs
    {
        public Song? Song { get; }

        public MediaEndedEventArgs(Song? song)
        {
            Song = song;
        }
    }
}
