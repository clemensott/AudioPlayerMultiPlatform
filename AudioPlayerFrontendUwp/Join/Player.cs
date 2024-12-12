using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using AudioPlayerBackend.Player;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private int setSourceCount = 0;
        private PlaybackState playState;
        private RequestSong? request;
        private RequestSong setRequest;
        private readonly SemaphoreSlim sem;
        private readonly MediaPlayer player;

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

        public TimeSpan Position => player.PlaybackSession.Position;

        public TimeSpan Duration => player.PlaybackSession.NaturalDuration;

        public Song? Source { get; private set; }

        public float Volume { get => (float)player.Volume; set => player.Volume = value; }

        public double PlaybackRate
        {
            get => player.PlaybackSession.PlaybackRate;
            set => player.PlaybackSession.PlaybackRate = value;
        }

        public SystemMediaTransportControls SMTC => player.SystemMediaTransportControls;

        public Player()
        {
            sem = new SemaphoreSlim(1);
            player = new MediaPlayer();
            player.MediaOpened += Player_MediaOpened;
            player.MediaFailed += Player_MediaFailed;
            player.MediaEnded += Player_MediaEnded;
            player.VolumeChanged += Player_VolumeChanged;

            player.CommandManager.IsEnabled = true;
            player.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.NextReceived += CommandManager_NextReceived;
            player.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            player.CommandManager.PlayReceived += CommandManager_PlayReceived;
            player.CommandManager.PauseReceived += CommandManager_PauseReceived;
        }

        private void Player_VolumeChanged(MediaPlayer sender, object args)
        {
            VolumeChanged?.Invoke(this, new ValueChangedEventArgs<float>(-1, Volume));
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            HandledEventArgs subArgs = new HandledEventArgs(args.Handled);
            NextPressed?.Invoke(this, subArgs);

            args.Handled = subArgs.Handled;
        }

        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            HandledEventArgs subArgs = new HandledEventArgs(args.Handled);
            PreviousPressed?.Invoke(this, subArgs);

            args.Handled = subArgs.Handled;
        }

        private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
        {
            PlayState = PlaybackState.Playing;
            args.Handled = true;
        }

        private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
        {
            PlayState = PlaybackState.Paused;
            args.Handled = true;
        }

        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
            Source = setRequest.Song;
            AudioPlayerBackend.Logs.Log("Player.Player_MediaOpened1", setRequest.Song.FullPath, setSourceCount);
            if (setRequest.Duration == sender.PlaybackSession.NaturalDuration)
            {
                sender.PlaybackSession.Position = setRequest.Position;
            }

            MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, setRequest.Song));
            ExecutePlayState();
            sem.Release();
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaFailed1", setRequest.Song.FullPath, setSourceCount);
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequest.Song, args.ExtendedErrorCode));
            sem.Release();
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaEnded1", Source?.FullPath, setSourceCount);
            MediaEnded?.Invoke(this, new MediaEndedEventArgs(Source));
        }

        public Task Set(RequestSong? request)
        {
            return request.HasValue ? Set(request.Value) : Stop();
        }

        private async Task Set(RequestSong request)
        {
            this.request = request;

            await sem.WaitAsync();

            bool release = true;
            try
            {
                if (!this.request.Equals(request)) return;

                if (Source.HasValue && request.Song.FullPath == Source?.FullPath)
                {
                    if (!request.ContinuePlayback && request.Position != Position)
                    {
                        player.PlaybackSession.Position = request.Position;
                    }
                    setSourceCount++;
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
                if (release) sem.Release();
            }

            try
            {
                setRequest = request;
                AudioPlayerBackend.Logs.Log("Player.Set5", request.Song.FullPath, request.ContinuePlayback, setSourceCount);
                StorageFile file = await StorageFile.GetFileFromPathAsync(request.Song.FullPath);
                AudioPlayerBackend.Logs.Log("Player.Set6", file.Path, setSourceCount);
                player.Source = MediaSource.CreateFromStorageFile(file);

                await SMTC.DisplayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Title))
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = request.Song.Title ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Artist))
                {
                    SMTC.DisplayUpdater.MusicProperties.Artist = request.Song.Artist ?? string.Empty;
                }
                SMTC.DisplayUpdater.Update();
            }
            catch (Exception e)
            {
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
                sem.Release();
            }
        }

        public async Task Stop()
        {
            request = null;
            await sem.WaitAsync();

            try
            {
                player.Source = null;
                Source = null;
            }
            finally
            {
                sem.Release();
            }
        }

        private async void HandlePlayStateChange()
        {
            await sem.WaitAsync();

            try
            {
                ExecutePlayState();
            }
            finally
            {
                sem.Release();
            }
        }

        public void ExecutePlayState()
        {
            switch (PlayState)
            {
                case PlaybackState.Playing:
                    player.Play();
                    break;

                case PlaybackState.Paused:
                    player.Pause();
                    break;
            }
        }

        public void Dispose()
        {
            player.MediaOpened -= Player_MediaOpened;
            player.MediaFailed -= Player_MediaFailed;
            player.MediaEnded -= Player_MediaEnded;

            Source = null;

            player.Dispose();
        }
    }
}
