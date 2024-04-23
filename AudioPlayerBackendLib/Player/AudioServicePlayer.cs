using AudioPlayerBackend.Audio;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public class AudioServicePlayer : IServicePlayer
    {
        private const int updateInterval = 100;

        private bool isSetCurrentSong;
        private int errorCount;
        private readonly Timer timer;

        public IAudioService Service { get; }

        public IPlayer Player { get; }

        public IInvokeDispatcherService Dispatcher { get; }

        public AudioServicePlayer(IAudioService service, IPlayer player)
        {
            Service = service;
            Player = player;
            Dispatcher = AudioPlayerServiceProvider.Current.GetDispatcher();

            player.PlayState = service.PlayState;
            player.MediaOpened += Player_MediaOpened;
            player.MediaFailed += Player_MediaFailed;
            player.MediaEnded += Player_MediaEnded;

            timer = new Timer(Timer_Elapsed, null, -1, updateInterval);

            service.Volume = player.Volume;

            Subscribe(Service);
            CheckCurrentSong(Service.CurrentPlaylist);

            CheckUpdateCurrentSong();
        }

        private async void CheckUpdateCurrentSong()
        {
            if (!Player.Source.HasValue ^ Service.CurrentPlaylist?.CurrentSong == null)
            {
                await UpdateCurrentSong();
            }
        }

        private void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            errorCount = 0;

            IPlaylist currentPlaylist = Service.CurrentPlaylist;
            if (currentPlaylist?.WannaSong?.Song == e.Source)
            {
                currentPlaylist.CurrentSong = e.Source;
                currentPlaylist.Position = e.Position;
                currentPlaylist.Duration = e.Duration;
            }

            EnableTimer();
        }

        private void Player_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            StopTimer();
            if ( ++errorCount < 10) Service.Continue(e.Song);
        }

        private void Player_MediaEnded(object sender, MediaEndedEventArgs e)
        {
            Logs.Log($"Player_MediaEnded: {e.Song?.FullPath}");
            StopTimer();
            Service.Continue(e.Song);
        }

        private void Timer_Elapsed(object state)
        {
            Dispatcher.InvokeDispatcher(UpdatePosition);
        }

        private void UpdatePosition()
        {
            try
            {
                if (!Player.Source.HasValue ||
                    Player.Source != Service.CurrentPlaylist?.CurrentSong) return;

                TimeSpan position = Service.CurrentPlaylist.Position;
                if (Service.CurrentPlaylist.Position.Seconds == Player.Position.Seconds) return;

                Service.CurrentPlaylist.Position = Player.Position;
                Service.CurrentPlaylist.Duration = Player.Duration;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private void Subscribe(IAudioService service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged += Service_CurrentPlaylistChanged;
            service.PlayStateChanged += Service_PlayStateChanged;
            service.VolumeChanged += Service_VolumeChanged;

            Subscribe(service.CurrentPlaylist);
        }

        private void Unsubscribe(IAudioService service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged -= Service_CurrentPlaylistChanged;
            service.PlayStateChanged -= Service_PlayStateChanged;
            service.VolumeChanged -= Service_VolumeChanged;

            Unsubscribe(service.CurrentPlaylist);
        }

        private void Subscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.WannaSongChanged += Playlist_WannaSongChanged;
            playlist.SongsChanged += Playlist_SongsChanged;
        }

        private void Unsubscribe(IPlaylist playlist)
        {
            if (playlist == null) return;

            playlist.WannaSongChanged -= Playlist_WannaSongChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;
        }

        private async void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            Unsubscribe((IPlaylist)e.OldValue);
            Subscribe((IPlaylist)e.NewValue);

            if (e.OldValue != null)
            {
                e.OldValue.WannaSong = RequestSong.Get(e.OldValue.CurrentSong, e.OldValue.Position, e.OldValue.Duration);
            }

            CheckCurrentSong(Service.CurrentPlaylist);
            await UpdateCurrentSong();
        }

        private void Service_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            Player.PlayState = Service.PlayState;

            EnableTimer();
        }

        private void Service_VolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            Player.Volume = Service.Volume;
        }

        private async void Playlist_WannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            if (e.NewValue.HasValue) await UpdateCurrentSong();
        }

        private void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            CheckCurrentSong((IPlaylist)sender);
        }

        private static void CheckCurrentSong(IPlaylist playlist)
        {
            if (playlist == null) return;

            if (playlist.Songs == null || playlist.Songs.Length == 0) playlist.WannaSong = null;
            else if (!playlist.WannaSong.HasValue || !playlist.Songs.Contains(playlist.WannaSong.Value.Song))
            {
                playlist.WannaSong = RequestSong.Start(playlist.Songs.First());
            }
        }

        private async Task UpdateCurrentSong()
        {
            StopTimer();
            isSetCurrentSong = true;

            IPlaylist currentPlaylist = Service.CurrentPlaylist;
            RequestSong? wannaSong = currentPlaylist?.WannaSong;
            await Player.Set(wannaSong);

            if (currentPlaylist != null && currentPlaylist.WannaSong.Equals(wannaSong))
            {
                currentPlaylist.CurrentSong = wannaSong?.Song;
            }

            isSetCurrentSong = false;

            EnableTimer();
        }

        private void EnableTimer()
        {
            if (!isSetCurrentSong && Service.CurrentPlaylist?.CurrentSong != null &&
                Service.PlayState == PlaybackState.Playing) StartTimer();
            else StopTimer();

            UpdatePosition();
        }

        private void StartTimer()
        {
            timer?.Change(updateInterval, updateInterval);
        }

        private void StopTimer()
        {
            timer?.Change(-1, -1);
        }

        public void Dispose()
        {
            Player.MediaOpened -= Player_MediaOpened;
            Player.MediaFailed -= Player_MediaFailed;
            Player.MediaEnded -= Player_MediaEnded;
            Unsubscribe(Service);

            timer.Dispose();
            Player.Stop();
            Player.Dispose();
        }
    }
}
