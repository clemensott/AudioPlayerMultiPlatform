using System;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource
{
    public class FileMediaSourceRootWithPlaylists
    {
        public Guid Id { get; }

        public FileMediaSourceRootUpdateType UpdateType { get; }

        public string Name { get; }

        /// <summary>
        /// Type of value in propety Value.
        /// </summary>
        public FileMediaSourceRootPathType PathType { get; }

        /// <summary>
        /// Path in local file system. Can start with a known folder, e.g. "Music:\Classic"
        /// </summary>
        public string Path { get; }

        public PlaylistInfo[] Playlists { get; }

        public FileMediaSourceRootWithPlaylists(Guid id, FileMediaSourceRootUpdateType updateType, string name,
            FileMediaSourceRootPathType pathType, string path, PlaylistInfo[] playlists)
        {
            Id = id;
            UpdateType = updateType;
            Name = name;
            PathType = pathType;
            Path = path;
            Playlists = playlists;
        }
    }
}