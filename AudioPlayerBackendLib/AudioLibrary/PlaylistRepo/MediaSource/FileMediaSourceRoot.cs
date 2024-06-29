using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public struct FileMediaSourceRoot
    {
        public FileMediaSourceRootUpdateType UpdateType { get; }

        public string Name { get; }

        /// <summary>
        /// Type of value in propety Value.
        /// </summary>
        public FileMediaSourceRootType Type { get; }

        /// <summary>
        /// Name of KnownFolder or Path in local file system. Depending on Type.
        /// </summary>
        public string Value { get; }

        public ICollection<FileMediaSource> Sources { get; }
    }
}
