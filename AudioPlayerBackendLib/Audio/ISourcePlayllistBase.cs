using System;

namespace AudioPlayerBackend.Audio
{
    public interface ISourcePlaylistBase : IPlaylistBase
    {
        event EventHandler<ValueChangedEventArgs<string[]>> FileMediaSourcesChanged;

        string[] FileMediaSources { get; set; }
    }
}
