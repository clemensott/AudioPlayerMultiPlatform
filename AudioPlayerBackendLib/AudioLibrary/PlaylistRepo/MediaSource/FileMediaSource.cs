namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public struct FileMediaSource
    {
        public string RelativePath { get; set; }

        public FileMediaSource(string relativePath)
        {
            RelativePath = relativePath;
        }
    }
}
