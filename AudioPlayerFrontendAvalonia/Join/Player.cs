using System;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using AudioPlayerBackend.Player;
using LibVLCSharp.Shared;

namespace AudioPlayerFrontendAvalonia.Join;

public class Player : IPlayer
{
    private readonly LibVLC libVLC;
    private readonly MediaPlayer mediaPlayer;
    private PlaybackState playState;
    
    private RequestSong openingRequestSong;
    private RequestSong? setRequestSong;
    private readonly SemaphoreSlim handleSem;

    public event EventHandler<MediaOpenedEventArgs>? MediaOpened;
    public event EventHandler<MediaFailedEventArgs>? MediaFailed;
    public event EventHandler<MediaEndedEventArgs>? MediaEnded;
    public event EventHandler<HandledEventArgs>? NextPressed;
    public event EventHandler<HandledEventArgs>? PreviousPressed;
    public event EventHandler<ValueChangedEventArgs<PlaybackState>>? PlayStateChanged;
    public event EventHandler<ValueChangedEventArgs<float>>? VolumeChanged;

    public PlaybackState PlayState
    {
        get => playState;
        set
        {
            PlaybackState oldState = playState;
            playState = value;

            ExecutePlayState();

            if (oldState != value)
            {
                PlayStateChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackState>(oldState, value));
            }
        }
    }

    public double PlaybackRate { get; set; }

    public TimeSpan Position => TimeSpan.FromMilliseconds(mediaPlayer.Position * mediaPlayer.Length);

    public TimeSpan Duration => TimeSpan.FromMilliseconds(mediaPlayer.Length);

    public Song? Source { get; private set; }

    public float Volume
    {
        get => mediaPlayer.Volume / 100f;
        set => mediaPlayer.Volume = (int)(value * 100);
    }

    public Player()
    {
        libVLC = new LibVLC();
        mediaPlayer = new MediaPlayer(libVLC);
        mediaPlayer.Opening += OnOpening;
        mediaPlayer.EndReached += OnEndReached;

        handleSem = new SemaphoreSlim(1, 1);
    }

    private void OnOpening(object? sender, EventArgs e)
    {
        try
        {
            Source = openingRequestSong.Song;
            if (openingRequestSong.Position > TimeSpan.Zero
                && mediaPlayer.Length > 0
                && Math.Abs(openingRequestSong.Duration.TotalMilliseconds - mediaPlayer.Length) < 10)
            {
                mediaPlayer.SeekTo(openingRequestSong.Position);
            }

            MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, openingRequestSong.Song));
            ExecutePlayState();
        }
        finally
        {
            handleSem.Release();
        }
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        MediaEnded?.Invoke(this, new MediaEndedEventArgs(Source));
    }

    public Task<(TimeSpan position, TimeSpan duration)> GetTimesSafe()
    {
        return Task.FromResult((Position, Duration));
    }

    public async Task Set(RequestSong? request)
    {
        if (request is { } song)  await Set(song);
        else await Stop();
    }

    private async Task Set(RequestSong request)
    {
        setRequestSong = request;
        await handleSem.WaitAsync();

        bool release = true;
        try
        {
            if (!setRequestSong.Equals(request)) return;

            if (request.Song.FullPath == Source?.FullPath)
            {
                if (!request.ContinuePlayback && request.Position != Position)
                {
                    mediaPlayer.SeekTo(request.Position);
                }
                
                Source = request.Song;
                ExecutePlayState();
                return;
            }

            release = false;
        }
        catch (Exception e)
        {
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
        }
        finally
        {
            if (release) handleSem.Release();
        }
        
        try
        {
            Media media = new Media(libVLC, new Uri(request.Song.FullPath));
            openingRequestSong = request;
            await Task.Run(() =>
            {
                if (PlayState == PlaybackState.Playing) mediaPlayer.Play(media);
                else mediaPlayer.Media = media;
            });
        }
        catch (Exception e)
        {
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
            handleSem.Release();
        }
    }

    public async Task Stop()
    {
        await handleSem.WaitAsync();
        try
        {
            mediaPlayer.Stop();
        }
        finally
        {
            handleSem.Release();
        }
    }

    private void ExecutePlayState()
    {
        switch (PlayState)
        {
            case PlaybackState.Playing:
                mediaPlayer.Play();
                break;

            case PlaybackState.Paused:
                mediaPlayer.Pause();
                break;
        }
    }

    public void Dispose()
    {
        mediaPlayer.Dispose();
        libVLC.Dispose();
        handleSem.Dispose();
    }
}