using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioServiceBase
    {
        event EventHandler<ValueChangedEventArgs<IPlaylistBase>> CurrentPlaylistChanged;
        event EventHandler<ValueChangedEventArgs<ISourcePlaylistBase[]>> SourcePlaylistsChanged;
        event EventHandler<ValueChangedEventArgs<IPlaylistBase[]>> PlaylistsChanged;
        event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;
        event EventHandler<ValueChangedEventArgs<byte[]>> AudioDataChanged;
        event EventHandler<ValueChangedEventArgs<bool>> IsSearchShuffleChanged;
        event EventHandler<ValueChangedEventArgs<string>> SearchKeyChanged;

        bool IsSearchShuffle { get; set; }

        string SearchKey { get; set; }

        IPlaylistBase CurrentPlaylist { get; set; }

        ISourcePlaylistBase[] SourcePlaylists { get; set; }

        IPlaylistBase[] Playlists { get; set; }

        PlaybackState PlayState { get; set; }

        float Volume { get; set; }

        byte[] AudioData { get; set; }
    }
}