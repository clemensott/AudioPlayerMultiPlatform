using System;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontendAvalonia.Join;

// TODO: implement
public class Player : IPlayer
{
    public event EventHandler<MediaOpenedEventArgs>? MediaOpened;
    public event EventHandler<MediaFailedEventArgs>? MediaFailed;
    public event EventHandler<MediaEndedEventArgs>? MediaEnded;
    public event EventHandler<HandledEventArgs>? NextPressed;
    public event EventHandler<HandledEventArgs>? PreviousPressed;
    public event EventHandler<ValueChangedEventArgs<PlaybackState>>? PlayStateChanged;
    public event EventHandler<ValueChangedEventArgs<float>>? VolumeChanged;
    public PlaybackState PlayState { get; set; }
    public double PlaybackRate { get; set; }
    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }
    public Song? Source { get; }
    public float Volume { get; set; }

    public Task<(TimeSpan position, TimeSpan duration)> GetTimesSafe()
    {
        return Task.FromResult((TimeSpan.Zero, TimeSpan.Zero));
    }

    public Task Set(RequestSong? request)
    {
        return Task.CompletedTask;
        // throw new NotImplementedException();
    }

    public Task Stop()
    {
        return Task.CompletedTask;
        // throw new NotImplementedException();
    }

    public void Dispose()
    {
        // throw new NotImplementedException();
    }
}