using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public class RemovePlaylistArgs : EventArgs
    {
        public Guid Id { get; }

        public RemovePlaylistArgs(Guid id)
        {
            Id = id;
        }
    }
}
