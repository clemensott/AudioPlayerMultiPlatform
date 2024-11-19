using System;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class AudioLibraryChangeArgs<T> : EventArgs
    {
        public T NewValue { get; }

        public AudioLibraryChangeArgs(T newValue)
        {
            NewValue = newValue;
        }
    }
}
