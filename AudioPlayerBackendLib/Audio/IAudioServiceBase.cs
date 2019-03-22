using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioServiceBase
    {
        event EventHandler<ValueChangedEventArgs<IPlaylistBase>> CurrentPlaylistChanged;
        event EventHandler<ValueChangedEventArgs<IPlaylistBase[]>> PlaylistsChanged;
        event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;
        event EventHandler<ValueChangedEventArgs<WaveFormat>> AudioFormatChanged;
        event EventHandler<ValueChangedEventArgs<byte[]>> AudioDataChanged;

        ISourcePlaylistBase SourcePlaylist { get; }

        IPlaylistBase CurrentPlaylist { get; set; }

        IPlaylistBase[] Playlists { get; set; }

        PlaybackState PlayState { get; set; }

        float Volume { get; set; }

        WaveFormat AudioFormat { get; set; }

        byte[] AudioData { get; set; }
    }
}