using AudioPlayerBackend.Audio;
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
        private PlaybackState playState;
        private RequestSong? wannaSong;
        private readonly SemaphoreSlim sem;
        private readonly MediaPlayer player;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;
        public event EventHandler<MediaOpenedEventArgs> MediaOpened;
        public event EventHandler<HandledEventArgs> NextPressed;
        public event EventHandler<HandledEventArgs> PreviousPressed;
        public event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                PlaybackState oldState = playState;
                playState = value;

                HandlePlayStateChange();

                PlayStateChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackState>(oldState, value));
            }
        }

        public TimeSpan Position => player.PlaybackSession.Position;

        public TimeSpan Duration => player.PlaybackSession.NaturalDuration;

        public Song? Source { get; private set; }

        public float Volume { get => (float)player.Volume; set => player.Volume = value; }

        public SystemMediaTransportControls SMTC => player.SystemMediaTransportControls;

        public Player()
        {
            sem = new SemaphoreSlim(1);
            player = new MediaPlayer();
            player.MediaOpened += Player_MediaOpened;
            player.MediaFailed += Player_MediaFailed;
            player.MediaEnded += Player_MediaEnded;

            player.CommandManager.IsEnabled = true;
            player.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.NextReceived += CommandManager_NextReceived;
            player.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            player.CommandManager.PlayReceived += CommandManager_PlayReceived;
            player.CommandManager.PauseReceived += CommandManager_PauseReceived;
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
            Source = wannaSong.Value.Song;
            if (wannaSong.Value.Position.HasValue && wannaSong.Value.Duration == sender.PlaybackSession.NaturalDuration)
            {
                sender.PlaybackSession.Position = wannaSong.Value.Position.Value;
            }

            MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, wannaSong.Value.Song));
            ExecutePlayState();
            sem.Release();
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Source = null;
            PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(wannaSong?.Song, args.ExtendedErrorCode));
            sem.Release();
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(Source));
        }

        public Task Set(RequestSong? wanna)
        {
            return wanna.HasValue ? Set(wanna.Value) : Stop();
        }

        private async Task Set(RequestSong wanna)
        {
            wannaSong = wanna;

            await sem.WaitAsync();

            bool release = true;
            try
            {
                if (!wannaSong.Equals(wanna)) return;

                if (Source.HasValue && wanna.Song.FullPath == Source?.FullPath)
                {
                    if (wanna.Position.HasValue &&
                        wanna.Position.Value != Position)
                    {
                        player.PlaybackSession.Position = wanna.Position.Value;
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
                PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(wanna.Song, e));
            }
            finally
            {
                if (release) sem.Release();
            }

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(wanna.Song.FullPath);
                player.Source = MediaSource.CreateFromStorageFile(file);

                await SMTC.DisplayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Title))
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = wanna.Song.Title ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Artist))
                {
                    SMTC.DisplayUpdater.MusicProperties.Artist = wanna.Song.Artist ?? string.Empty;
                }
                SMTC.DisplayUpdater.Update();
            }
            catch (Exception e)
            {
                Source = null;
                PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(wanna.Song, e));
                sem.Release();
            }
        }

        public async Task Stop()
        {
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

            player.Dispose();
        }
    }
}
