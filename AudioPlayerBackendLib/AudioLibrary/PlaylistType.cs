namespace AudioPlayerBackend.AudioLibrary
{
    public enum PlaylistType : short
    {
        Custom = 1, // Any custom build playlist (not build from a file system source)
        Search = 2, // Custom Playlist that is build from a search
        SourcePlaylist = 4, // Playlist that is build from a file system source
        AutoSourcePlaylist = 8, // Source Playlist that was create by the auto updater
    }
}
