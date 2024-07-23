using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public struct FileMediaSourceRoot
    {
        public Guid Id { get; }

        public FileMediaSourceRootUpdateType UpdateType { get; }

        public string Name { get; }

        /// <summary>
        /// Type of value in propety Value.
        /// </summary>
        public FileMediaSourceRootType Type { get; }

        /// <summary>
        /// Path in local file system. Can start with a known folder, e.g. "Music:\Classic"
        /// </summary>
        public string Path { get; }

        public FileMediaSourceRoot(Guid id, FileMediaSourceRootUpdateType updateType, string name,
            FileMediaSourceRootType type, string path)
        {
            Id = id;
            UpdateType = updateType;
            Name = name;
            Type = type;
            Path = path;
        }
    }
}
