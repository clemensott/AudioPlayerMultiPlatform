using System;

namespace AudioPlayerBackend.AudioLibrary
{
    public class AudioLibraryChange<T> : EventArgs
    {
        public T NewValue { get; }

        public AudioLibraryChange(T newValue)
        {
            NewValue = newValue;
        }
    }
}
