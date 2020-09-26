using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerFrontend.Join
{
    class Player : IWaveProviderPlayer
    {
        public PlaybackState PlayState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void ExecutePlayState()
        {
            throw new NotImplementedException();
        }

        public void Play(Func<AudioPlayerBackend.Player.IWaveProvider> waveProviderFunc)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
