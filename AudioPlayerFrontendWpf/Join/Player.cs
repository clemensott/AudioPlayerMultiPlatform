using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using AudioPlayerBackend.Player;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private RequestSong setRequestSong;
        private RequestSong? requestSong;
        private PlaybackState playState;
        private readonly MediaElement mediaElement;
        private readonly SemaphoreSlim handleSem;
        private TimeSpan lastPosition;

        public event EventHandler<MediaOpenedEventArgs> MediaOpened;
        public event EventHandler<MediaFailedEventArgs> MediaFailed;
        public event EventHandler<MediaEndedEventArgs> MediaEnded;
        public event EventHandler<HandledEventArgs> NextPressed;
        public event EventHandler<HandledEventArgs> PreviousPressed;
        public event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                PlaybackState oldState = playState;
                playState = value;

                HandlePlayStateChange();

                if (oldState != value)
                {
                    PlayStateChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackState>(oldState, value));
                }
            }
        }

        public float Volume { get => (float)mediaElement.Volume; set => mediaElement.Volume = value; }

        public TimeSpan Position
        {
            get
            {
                TimeSpan current = mediaElement.Position;
                if (current > TimeSpan.Zero) lastPosition = current;
                return current;
            }
        }

        public TimeSpan Duration => mediaElement.NaturalDuration.HasTimeSpan ? mediaElement.NaturalDuration.TimeSpan : TimeSpan.Zero;

        public double PlaybackRate
        {
            get => mediaElement.SpeedRatio;
            set => mediaElement.SpeedRatio = value;
        }

        public Song? Source { get; private set; }

        public Player()
        {
            handleSem = new SemaphoreSlim(1);

            mediaElement = new MediaElement()
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
            };
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.MediaFailed += MediaElement_MediaFailed;

            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                AudioPlayerBackend.Logs.Log($"MediaElement_MediaOpened1: {setRequestSong.Song.FullPath}");
                Source = setRequestSong.Song;
                if (setRequestSong.Position > TimeSpan.Zero
                    && mediaElement.NaturalDuration.HasTimeSpan
                    && setRequestSong.Duration == mediaElement.NaturalDuration.TimeSpan)
                {
                    SetPosition(setRequestSong.Position);
                }
                else lastPosition = Position;

                MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, setRequestSong.Song));
                ExecutePlayState();
            }
            finally
            {
                handleSem.Release();
            }
        }

        private void SetPosition(TimeSpan pos)
        {
            if (PlayState == PlaybackState.Paused)
            {
                mediaElement.IsMuted = true;
                mediaElement.Play();
            }

            mediaElement.Position = pos;
            lastPosition = pos;

            if (PlayState == PlaybackState.Paused)
            {
                mediaElement.Pause();
                mediaElement.IsMuted = false;
            }
        }

        private void MediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            try
            {
                AudioPlayerBackend.Logs.Log($"MediaElement_MediaFailed1: {e.ErrorException}");
                AudioPlayerBackend.Logs.Log($"MediaElement_MediaFailed2: {Position} / {Duration} | {Source?.FullPath} | {PlayState}");
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequestSong.Song, e.ErrorException));
            }
            finally
            {
                handleSem.Release();
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            AudioPlayerBackend.Logs.Log($"MediaElement_MediaEnded: {Position} / {Duration} | {Source?.FullPath} | {PlayState}");
            MediaEnded?.Invoke(this, new MediaEndedEventArgs(Source));
        }

        private async void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            AudioPlayerBackend.Logs.Log($"OnPowerChange1: {e.Mode} | {PlayState} | {lastPosition} | {Position}");
            if (e.Mode == PowerModes.Resume && PlayState == PlaybackState.Playing && Source != null)
            {
                if (handleSem.CurrentCount == 0) handleSem.Release();
                RequestSong restartRequest = new RequestSong(Source.Value, lastPosition, Duration, false);
                await Stop();
                await Set(restartRequest);
            }
            else if (e.Mode == PowerModes.Suspend && Position > TimeSpan.Zero)
            {
                lastPosition = Position;
            }
        }

        public async Task<(TimeSpan position, TimeSpan duration)> GetTimesSafe()
        {
            return await mediaElement.Dispatcher.InvokeAsync(() => (Position, Duration));
        }

        public async Task SetSongs(ICollection<Song> songs)
        {
            
        }

        public Task Set(RequestSong? request)
        {
            return request.HasValue ? Set(request.Value) : Stop();
        }

        private async Task Set(RequestSong request)
        {
            requestSong = request;

            await handleSem.WaitAsync();

            await mediaElement.Dispatcher.InvokeAsync(() =>
            {
                bool release = true;
                try
                {
                    if (!requestSong.Equals(request)) return;

                    if (Source.HasValue && request.Song.FullPath == Source?.FullPath)
                    {
                        if (!request.ContinuePlayback && request.Position != Position)
                        {
                            SetPosition(request.Position);
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
                    AudioPlayerBackend.Logs.Log($"Player.Set8: {request.Song.FullPath}");
                    setRequestSong = request;
                    mediaElement.Source = new Uri(request.Song.FullPath);
                }
                catch (Exception e)
                {
                    Source = null;
                    MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
                    handleSem.Release();
                }
            });
        }

        public async Task Stop()
        {
            AudioPlayerBackend.Logs.Log($"Player.Stop2: {Source?.FullPath}");
            await handleSem.WaitAsync();
            try
            {
                AudioPlayerBackend.Logs.Log($"Player.Stop3: {Source?.FullPath}");
                mediaElement.Source = null;
                Source = null;
                lastPosition = TimeSpan.Zero;
            }
            finally
            {
                handleSem.Release();
            }
        }

        private async void HandlePlayStateChange()
        {
            await handleSem.WaitAsync();

            try
            {
                await mediaElement.Dispatcher.InvokeAsync(ExecutePlayState);
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
                    mediaElement.Play();
                    break;

                case PlaybackState.Paused:
                    mediaElement.Pause();
                    break;
            }
        }

        public void Dispose()
        {
            mediaElement.MediaOpened -= MediaElement_MediaOpened;
            mediaElement.MediaFailed -= MediaElement_MediaFailed;
            mediaElement.MediaEnded -= MediaElement_MediaEnded;

            mediaElement.Close();
        }
    }
}
