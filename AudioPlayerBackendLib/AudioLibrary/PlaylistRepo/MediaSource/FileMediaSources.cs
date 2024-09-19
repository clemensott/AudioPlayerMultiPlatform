using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public class FileMediaSources
    {
        public FileMediaSourceRoot Root { get; }

        public ICollection<FileMediaSource> Sources { get; }

        public FileMediaSources(FileMediaSourceRoot root, ICollection<FileMediaSource> sources)
        {
            Root = root;
            Sources = sources;
        }
    }
}
