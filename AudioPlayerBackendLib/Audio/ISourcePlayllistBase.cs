using AudioPlayerBackend.Audio.MediaSource;
using System;

namespace AudioPlayerBackend.Audio
{
    public interface ISourcePlaylistBase : IPlaylistBase
    {
        event EventHandler<ValueChangedEventArgs<FileMediaSource[]>> FileMediaSourcesChanged;

        FileMediaSource[] FileMediaSources { get; set; }
    }
}
