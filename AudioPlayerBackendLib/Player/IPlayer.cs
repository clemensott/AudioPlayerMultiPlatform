using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public interface IPlayer : IDisposable
    {
        event EventHandler<MediaOpenedEventArgs> MediaOpened;
        event EventHandler<MediaFailedEventArgs> MediaFailed;
        event EventHandler<MediaEndedEventArgs> MediaEnded;
        event EventHandler<HandledEventArgs> NextPressed;
        event EventHandler<HandledEventArgs> PreviousPressed;
        event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;

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
