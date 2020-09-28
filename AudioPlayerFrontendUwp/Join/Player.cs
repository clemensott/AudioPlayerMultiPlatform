using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        public PlaybackState PlayState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TimeSpan Position => throw new NotImplementedException();

        public TimeSpan Duration => throw new NotImplementedException();

        public Song? Source => throw new NotImplementedException();

        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;
        public event EventHandler<MediaOpenedEventArgs> MediaOpened;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task Set(RequestSong? wanna)
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
