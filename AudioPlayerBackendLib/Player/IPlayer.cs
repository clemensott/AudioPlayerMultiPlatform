using AudioPlayerBackend.Audio;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public interface IPlayer : IDisposable
    {
        event EventHandler<MediaOpenedEventArgs> MediaOpened;
        event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

        PlaybackState PlayState { get; set; }

        TimeSpan Position { get; }

        TimeSpan Duration { get; }

        Song? Source { get; }

        float Volume { get; set; }

        Task Set(RequestSong? wanna);

        Task Stop();
    }
}
