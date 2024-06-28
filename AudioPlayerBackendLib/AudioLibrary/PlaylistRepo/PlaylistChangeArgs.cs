using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public class PlaylistChangeArgs<T> : EventArgs
    {
        public Guid Id { get; }

        public T NewValue { get; }

        public PlaylistChangeArgs(Guid id, T newValue)
        {
            Id = id;
            NewValue = newValue;
        }
    }
}
