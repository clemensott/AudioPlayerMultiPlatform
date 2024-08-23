using System.IO;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public struct FileMediaSource
    {
        public string RelativePath { get; set; }

        public FileMediaSource(string relativePath)
        {
            RelativePath = relativePath;
        }

        public static string NormalizeRelativePath(string relativePath)
        {
            return relativePath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
