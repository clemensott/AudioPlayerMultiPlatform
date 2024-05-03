using System;

namespace AudioPlayerBackend.Audio
{
    public struct FileMediaSourceRoot
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Type of value in propety Value.
        /// This property is only relevant for local use and not for connected servers or clients.
        /// Because it's only used to find files/songs in local file system and has to be selected by user on every device/instance.
        /// </summary>
        public FileMediaSourceRootType Type { get; set; }

        /// <summary>
        /// Name of KnownFolder or Path in local file system. Depending on Type.
        /// This property is only relevant for local use and not for connected servers or clients.
        /// Because it's only used to find files/songs in local file system and has to be selected by user on every device/instance.
        /// </summary>
        public string Value { get; set; }
    }
}
