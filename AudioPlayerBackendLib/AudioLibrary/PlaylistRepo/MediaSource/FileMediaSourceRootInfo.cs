using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public struct FileMediaSourceRootInfo
    {
        public FileMediaSourceRootUpdateType UpdateType { get; }

        public string Name { get; }

        public FileMediaSourceRootPathType PathType { get; }

        public string Path { get; }

        public FileMediaSourceRootInfo(FileMediaSourceRootUpdateType updateType, string name, FileMediaSourceRootPathType pathType, string path) : this()
        {
            UpdateType = updateType;
            Name = name;
            PathType = pathType;
            Path = path;
        }

        public FileMediaSourceRoot CreateRoot()
        {
            return new FileMediaSourceRoot(Guid.NewGuid(), UpdateType, Name, PathType, Path);
        }
    }
}
