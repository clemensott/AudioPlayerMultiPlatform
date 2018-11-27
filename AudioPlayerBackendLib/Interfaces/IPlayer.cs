using AudioPlayerBackend.Common;
using System;

namespace AudioPlayerBackend
{
    public interface IPlayer
    {
        event EventHandler<StoppedEventArgs> PlaybackStopped;

        PlaybackState PlayState { get; set; }
        float Volume { get; set; }
        void Play(IWaveProvider waveProvider);
        void Stop(IWaveProvider waveProvider);
    }
}
