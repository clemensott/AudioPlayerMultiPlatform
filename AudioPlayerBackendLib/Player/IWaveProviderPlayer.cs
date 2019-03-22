using System;

namespace AudioPlayerBackend.Player
{
    public interface IWaveProviderPlayer : IDisposable
    {
        event EventHandler<StoppedEventArgs> PlaybackStopped;

        PlaybackState PlayState { get; set; }
        float Volume { get; set; }
        void Play(Func<IWaveProvider> waveProviderFunc);
        void Stop();
        void ExecutePlayState();
    }
}
