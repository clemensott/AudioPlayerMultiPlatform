using AudioPlayerBackend.Common;
using System;

namespace AudioPlayerBackend
{
    public interface IPlayer
    {
        event EventHandler<StoppedEventArgs> PlaybackStopped;

        PlaybackState PlayState { get; set; }
        float Volume { get; set; }
        void Play(Func<IWaveProvider> waveProviderFunc);
        void Stop(IDisposable waveProvider);
        void ExecutePlayState();
    }
}
