using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private RequestSong? wannaSong;
        private PlaybackState playState;
        private readonly MediaElement mediaElement;
        private readonly SemaphoreSlim handleSem;
        private TimeSpan lastPosition;

        public event EventHandler<MediaOpenedEventArgs> MediaOpened;
        public event EventHandler<MediaFailedEventArgs> MediaFailed;
        public event EventHandler<MediaEndedEventArgs> MediaEnded;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                PlaybackState oldState = playState;
                playState = value;

                HandlePlayStateChange();
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

        public Song? Source { get; private set; }

        public Player(int deviceNumber = -1, IntPtr? windowHandle = null)
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
            Source = wannaSong.Value.Song;
            if (wannaSong.Value.Position.HasValue && mediaElement.NaturalDuration.HasTimeSpan &&
                wannaSong.Value.Duration == mediaElement.NaturalDuration.TimeSpan)
            {
                SetPosition(wannaSong.Value.Position.Value);
            }
            else lastPosition = Position;

            MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, wannaSong.Value.Song));
            ExecutePlayState();
            handleSem.Release();
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
            AudioPlayerBackend.Logs.Log($"MediaElement_MediaFailed1: {e.ErrorException}");
            AudioPlayerBackend.Logs.Log($"MediaElement_MediaFailed2: {Position} / {Duration} | {Source?.FullPath} | {PlayState}");
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(wannaSong?.Song, e.ErrorException));
            handleSem.Release();
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
                RequestSong restartRequest = RequestSong.Get(Source.Value, lastPosition, Duration);
                await Stop();
                await Set(restartRequest);
            }
            else if (e.Mode == PowerModes.Suspend && Position > TimeSpan.Zero)
            {
                lastPosition = Position;
            }
        }

        public Task Set(RequestSong? wanna)
        {
            AudioPlayerBackend.Logs.Log($"Player.Set2: {wanna.HasValue}");
            return wanna.HasValue ? Set(wanna.Value) : Stop();
        }

        private async Task Set(RequestSong wanna)
        {
            AudioPlayerBackend.Logs.Log($"Player.Set4: {wanna.Song.FullPath} | {wanna.Position} / {wanna.Duration}");
            wannaSong = wanna;

            await handleSem.WaitAsync();

            bool release = true;
            try
            {
                AudioPlayerBackend.Logs.Log($"Player.Set5: {wannaSong.Equals(wanna)}");
                if (!wannaSong.Equals(wanna)) return;

                AudioPlayerBackend.Logs.Log($"Player.Set6: {Source.HasValue} | {wanna.Song.FullPath} | {Source?.FullPath}");
                if (Source.HasValue && wanna.Song.FullPath == Source?.FullPath)
                {
                    if (wanna.Position.HasValue &&
                        wanna.Position.Value != Position)
                    {
                        SetPosition(wanna.Position.Value);
                    }
                    Source = wanna.Song;
                    ExecutePlayState();
                    return;
                }
                release = false;
            }
            catch (Exception e)
            {
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(wanna.Song, e));
            }
            finally
            {
                if (release) handleSem.Release();
            }

            try
            {
                AudioPlayerBackend.Logs.Log($"Player.Set8: {wanna.Song.FullPath}");
                mediaElement.Source = new Uri(wanna.Song.FullPath);
            }
            catch (Exception e)
            {
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(wanna.Song, e));
                handleSem.Release();
            }
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
                ExecutePlayState();
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
