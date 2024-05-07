using System;

namespace AudioPlayerBackend.Audio.MediaSource
{
    public struct FileMediaSource
    {
        public string RelativePath { get; set; }

        public Guid RootId { get; set; }
    }
}
