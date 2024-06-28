using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public interface IPlayer : IDisposable
    {
        event EventHandler<MediaOpenedEventArgs> MediaOpened;
        event EventHandler<MediaFailedEventArgs> MediaFailed;
        event EventHandler<MediaEndedEventArgs> MediaEnded;

        PlaybackState PlayState { get; set; }

        double PlaybackRate { get; set; }

        TimeSpan Position { get; }

        TimeSpan Duration { get; }

        Song? Source { get; }

        float Volume { get; set; }

        Task Set(RequestSong? wanna);

        Task Stop();
    }
}
