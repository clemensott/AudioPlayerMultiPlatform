namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public enum FileMediaSourceRootUpdateType : short
    {
        None = 0,
        Songs = 1, // updates files and reloads songs of a playlist
        Folders = 2, // updates folders and creates or deletes playlists
    }
}
