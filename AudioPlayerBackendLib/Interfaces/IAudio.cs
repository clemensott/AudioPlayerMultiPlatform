using AudioPlayerBackend.Common;
using System;
using System.Collections.ObjectModel;

namespace AudioPlayerBackend
{
    public interface IAudio
    {
        IPlaylistExtended FileBasePlaylist { get; }

        IPlaylistExtended CurrentPlaylist { get; set; }

        ObservableCollection<IPlaylistExtended> AdditionalPlaylists { get; }

        string[] FileMediaSources { get; set; }

        PlaybackState PlayState { get; set; }

        float Volume { get; set; }
    }
}