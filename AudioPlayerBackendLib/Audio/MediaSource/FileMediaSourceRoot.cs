using System;

namespace AudioPlayerBackend.Audio.MediaSource
{
    public struct FileMediaSourceRoot
    {
        public Guid Id { get; set; }

        public FileMediaSourceRootUpdateType UpdateType { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Type of value in propety Value.
        /// </summary>
        public FileMediaSourceRootType Type { get; set; }

        /// <summary>
        /// Name of KnownFolder or Path in local file system. Depending on Type.
        /// </summary>
        public string Value { get; set; }
    }
}
