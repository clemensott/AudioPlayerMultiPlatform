using AudioPlayerBackend.Audio;
using StdOttStandard.Linq;
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
        private readonly IAudioServicePlayerHelper helper;
        private readonly Timer timer;

        public IAudioService Service { get; }

        public IPlayer Player { get; }

        public AudioServicePlayer(IAudioService service, IPlayer player, IAudioServicePlayerHelper helper = null)
        {
            Service = service;
            Player = player;
            this.helper = helper;

            player.PlayState = service.PlayState;
            player.MediaOpened += Player_MediaOpened;
            player.PlaybackStopped += Player_PlaybackStopped;

            timer = new Timer(Timer_Elapsed, null, updateInterval, updateInterval);

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
            IPlaylist currentPlaylist = Service.CurrentPlaylist;
            if (currentPlaylist?.WannaSong?.Song != e.Source) return;

            currentPlaylist.CurrentSong = e.Source;
            currentPlaylist.Position = e.Position;
            currentPlaylist.Duration = e.Duration;
        }

        private void Player_PlaybackStopped(object sender, PlaybackStoppedEventArgs e)
        {
            if (e.Exception == null && (Player.PlayState != PlaybackState.Stopped ||
                Player.Position >= Player.Duration)) Service.Continue();
        }

        private void Timer_Elapsed(object state)
        {
            try
            {
                if (!Player.Source.HasValue) return;

                TimeSpan position = Service.CurrentPlaylist.Position;
                if (Service.CurrentPlaylist.Position.Seconds == Player.Position.Seconds) return;

                Service.CurrentPlaylist.Position = Player.Position;
            }
            catch { }
        }

        private void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged += Service_CurrentPlaylistChanged;
            service.SourcePlaylistsChanged += Service_SourcePlaylistsChanged;
            service.PlayStateChanged += Service_PlayStateChanged;
            service.VolumeChanged += Service_VolumeChanged;

            Subscribe(service.CurrentPlaylist);

            service.SourcePlaylists.ForEach(Subscribe);
        }

        private void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged -= Service_CurrentPlaylistChanged;
            service.SourcePlaylistsChanged += Service_SourcePlaylistsChanged;
            service.PlayStateChanged -= Service_PlayStateChanged;
            service.VolumeChanged -= Service_VolumeChanged;

            Unsubscribe(service.CurrentPlaylist);

            service.SourcePlaylists.ForEach(Unsubscribe);
        }

        private void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.WannaSongChanged += Playlist_WannaSongChanged;
            playlist.SongsChanged += Playlist_SongsChanged;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.WannaSongChanged -= Playlist_WannaSongChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;
        }

        private void Subscribe(ISourcePlaylistBase playlist)
        {
            if (playlist != null) playlist.FileMediaSourcesChanged += Playlist_FileMediaSourcesChanged;
        }

        private void Unsubscribe(ISourcePlaylistBase playlist)
        {
            if (playlist != null) playlist.FileMediaSourcesChanged -= Playlist_FileMediaSourcesChanged;
        }

        private async void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            if (e.OldValue != null)
            {
                e.OldValue.WannaSong = RequestSong.Get(e.OldValue.CurrentSong, e.OldValue.Position, e.OldValue.Duration);
            }

            CheckCurrentSong(Service.CurrentPlaylist);
            await UpdateCurrentSong();
        }

        private void Service_SourcePlaylistsChanged(object sender, ValueChangedEventArgs<ISourcePlaylistBase[]> e)
        {
            e.OldValue.ForEach(Unsubscribe);
            e.NewValue.ForEach(Subscribe);

            foreach (ISourcePlaylistBase playlist in e.NewValue.ToNotNull().Except(e.OldValue.ToNotNull()))
            {
                helper?.Update(playlist);
            }
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

        private void Playlist_FileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
        {
            helper.Reload((ISourcePlaylistBase)sender);
        }

        private async void Playlist_WannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            if (e.NewValue.HasValue) await UpdateCurrentSong();
        }

        private void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            CheckCurrentSong((IPlaylistBase)sender);
        }

        private static void CheckCurrentSong(IPlaylistBase playlist)
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
            Player.PlaybackStopped -= Player_PlaybackStopped;
            Unsubscribe(Service);

            timer.Dispose();
            Player.Stop();
        }
    }
}
