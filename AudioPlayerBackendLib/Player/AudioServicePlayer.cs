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
        private readonly SemaphoreSlim setWannaSongSem;
        private readonly IAudioServicePlayerHelper helper;
        private readonly Timer timer;
        private ReadEventWaveProvider waveProvider;
        private string currentReaderPath;

        public IPositionWaveProvider Reader { get; private set; }

        public IAudioService Service { get; }

        public IWaveProviderPlayer Player { get; }

        public AudioServicePlayer(IAudioService service, IWaveProviderPlayer player, IAudioServicePlayerHelper helper = null)
        {
            setWannaSongSem = new SemaphoreSlim(1);
            Service = service;
            Player = player;
            this.helper = helper;

            player.PlayState = service.PlayState;
            player.PlaybackStopped += Player_PlaybackStopped;

            timer = new Timer(Timer_Elapsed, null, updateInterval, updateInterval);

            service.Volume = player.Volume;

            Subscribe(Service);
            CheckCurrentSong(Service.CurrentPlaylist);

            CheckUpdateCurrentSong();
        }

        private async void CheckUpdateCurrentSong()
        {
            if (Reader == null ^ Service.CurrentPlaylist?.CurrentSong == null)
            {
                await UpdateCurrentSong();
            }
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception == null && (Player.PlayState != PlaybackState.Stopped ||
                Reader.CurrentTime >= Reader.TotalTime)) Service.Continue();
        }

        private void Timer_Elapsed(object state)
        {
            try
            {
                if (Reader != null && Service.CurrentPlaylist.Position.Seconds != Reader.CurrentTime.Seconds)
                {
                    Service.CurrentPlaylist.Position = Reader.CurrentTime;
                }
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

        private Task UpdateCurrentSong()
        {
            IPlaylistBase currentPlaylist = Service.CurrentPlaylist;
            RequestSong? wannaSong = currentPlaylist?.WannaSong;

            return Task.Run(async () =>
            {
                await setWannaSongSem.WaitAsync();

                try
                {
                    if (currentPlaylist != Service.CurrentPlaylist || !wannaSong.Equals(currentPlaylist?.WannaSong)) return;

                    StopTimer();
                    isSetCurrentSong = true;

                    SetWannaSongThreadSafe(currentPlaylist, wannaSong);

                    isSetCurrentSong = false;
                    EnableTimer();
                }
                finally
                {
                    setWannaSongSem.Release();
                }
            });
        }

        protected virtual void SetWannaSongThreadSafe(IPlaylistBase currentPlaylist, RequestSong? wannaSong)
        {
            if (helper?.SetWannaSongThreadSafe != null)
            {
                helper.SetWannaSongThreadSafe(this);
                return;
            }

            if (Reader != null)
            {
                if (wannaSong.HasValue && wannaSong.Value.Song.FullPath == currentReaderPath)
                {
                    if (wannaSong.Value.Position.HasValue &&
                        wannaSong.Value.Position.Value != Reader.CurrentTime)
                    {
                        Reader.CurrentTime = wannaSong.Value.Position.Value;
                    }

                    currentPlaylist.CurrentSong = wannaSong.Value.Song;
                    currentPlaylist.Duration = Reader.TotalTime;
                    return;
                }

                Player.Stop();
            }

            try
            {
                if (wannaSong.HasValue)
                {
                    currentPlaylist.CurrentSong = wannaSong.Value.Song;
                    Player.Play(() => GetWaveProvider(currentPlaylist, wannaSong.Value));
                }
                else
                {
                    Reader = null;
                    Service.AudioFormat = null;
                    if (currentPlaylist != null) currentPlaylist.CurrentSong = null;
                }
            }
            catch
            {
                Reader = null;
                Service.AudioFormat = null;
            }
        }

        private IPositionWaveProvider GetWaveProvider(IPlaylistBase currentPlaylist, RequestSong wannaSong)
        {
            currentReaderPath = wannaSong.Song.FullPath;
            Reader = ToWaveProvider(CreateWaveProvider(wannaSong.Song));

            if (wannaSong.Position.HasValue &&
                Reader.TotalTime == wannaSong.Duration &&
                Reader.TotalTime > wannaSong.Position.Value)
            {
                Reader.CurrentTime = wannaSong.Position.Value;
            }

            currentPlaylist.Position = Reader.CurrentTime;
            currentPlaylist.Duration = Reader.TotalTime;

            return Reader;
        }

        private IPositionWaveProvider ToWaveProvider(IPositionWaveProvider waveProvider)
        {
            if (this.waveProvider != null) this.waveProvider.ReadEvent -= WaveProvider_Read;

            Service.AudioFormat = waveProvider.WaveFormat;

            this.waveProvider = new ReadEventWaveProvider(waveProvider);
            this.waveProvider.ReadEvent += WaveProvider_Read;

            return this.waveProvider;
        }

        private void WaveProvider_Read(object sender, WaveProviderReadEventArgs e)
        {
            Task.Factory.StartNew(() => Service.AudioData = e.Buffer.Skip(e.Offset).Take(e.ReturnCount).ToArray());
        }

        protected virtual IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return helper.CreateWaveProvider(song, Service);
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
            Player.PlaybackStopped -= Player_PlaybackStopped;
            timer.Dispose();

            Player?.Stop();

            if (Reader != null)
            {
                Reader?.Dispose();
                Reader = null;
            }

            Unsubscribe(Service);
        }
    }
}
