using System;

namespace AudioPlayerBackend.Audio
{
    public interface ISourcePlaylistBase : IPlaylistBase
    {
        event EventHandler<ValueChangedEventArgs<bool>> IsSearchShuffleChanged;
        event EventHandler<ValueChangedEventArgs<string>> SearchKeyChanged;
        event EventHandler<ValueChangedEventArgs<string[]>> FileMediaSourcesChanged;

        bool IsSearchShuffle { get; set; }

        string SearchKey { get; set; }

        string[] FileMediaSources { get; set; }
    }
}
