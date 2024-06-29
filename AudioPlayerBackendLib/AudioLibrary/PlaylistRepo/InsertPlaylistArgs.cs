using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public class InsertPlaylistArgs : EventArgs
    {
        public int Index { get; set; }

        public Playlist Playlist { get; set; }

        public InsertPlaylistArgs(int index, Playlist playlist)
        {
            Index = index;
            Playlist = playlist;
        }
    }
}
