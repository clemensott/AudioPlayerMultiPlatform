using System;

namespace AudioPlayerBackend.AudioLibrary
{
    public class PlaylistChange<T> : EventArgs
    {
        public Guid Id { get; }

        public T NewValue { get; }

        public PlaylistChange(Guid id, T newValue)
        {
            Id = id;
            NewValue = newValue;
        }
    }
}
