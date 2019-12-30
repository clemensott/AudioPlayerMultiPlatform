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
        private static readonly Random ran = new Random();

        private bool isSetCurrentSong;
        private RequestSong? currentWannaSong, nextWannaSong;
        private SemaphoreSlim setWannaSongSem;
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
                await UpdateCurrentSong(Service.CurrentPlaylist?.WannaSong);
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
            service.PlayStateChanged += Service_PlayStateChanged;
            service.VolumeChanged += Service_VolumeChanged;
            service.SourcePlaylist.FileMediaSourcesChanged += SourcePlaylist_FileMediaSourcesChanged;

            Subscribe(service.CurrentPlaylist);
        }

        private void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged -= Service_CurrentPlaylistChanged;
            service.PlayStateChanged -= Service_PlayStateChanged;
            service.VolumeChanged -= Service_VolumeChanged;
            service.SourcePlaylist.FileMediaSourcesChanged -= SourcePlaylist_FileMediaSourcesChanged;

            Unsubscribe(service.CurrentPlaylist);
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

        private async void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            e.OldValue.WannaSong = RequestSong.Get(e.OldValue.CurrentSong, e.OldValue.Position, e.OldValue.Duration);

            CheckCurrentSong(Service.CurrentPlaylist);
            await UpdateCurrentSong(e.NewValue.WannaSong);
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

        private void SourcePlaylist_FileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
        {
            Service.SourcePlaylist.Reload();
        }

        private async void Playlist_WannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            if (e.NewValue.HasValue) await UpdateCurrentSong(e.NewValue.Value);
        }

        private void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            CheckCurrentSong((IPlaylistBase)sender);
        }

        private static void CheckCurrentSong(IPlaylistBase playlist)
        {
            if (playlist.Songs == null || playlist.Songs.Length == 0) playlist.WannaSong = null;
            else if (!playlist.WannaSong.HasValue || !playlist.Songs.Contains(playlist.WannaSong.Value.Song))
            {
                playlist.WannaSong = RequestSong.Get(playlist.Songs.First());
            }
        }

        private async Task UpdateCurrentSong(RequestSong? wannaSong)
        {
            nextWannaSong = wannaSong;
            await setWannaSongSem.WaitAsync();

            if (nextWannaSong.Equals(wannaSong))
            {
                StopTimer();

                isSetCurrentSong = true;
                currentWannaSong = wannaSong;

                await Task.Factory.StartNew(SetWannaSongThreadSafe);

                isSetCurrentSong = false;

                EnableTimer();
            }

            setWannaSongSem.Release();
        }

        protected virtual void SetWannaSongThreadSafe()
        {
            if (helper?.SetWannaSongThreadSafe != null)
            {
                helper.SetWannaSongThreadSafe(this);
                return;
            }

            if (Reader != null)
            {
                if (currentWannaSong.HasValue && currentWannaSong.Value.Song.FullPath == currentReaderPath)
                {
                    if (Math.Abs((currentWannaSong.Value.Position - Reader.CurrentTime).TotalMilliseconds) > 200)
                    {
                        Reader.CurrentTime = currentWannaSong.Value.Position;
                    }

                    Service.CurrentPlaylist.CurrentSong = currentWannaSong.Value.Song;
                    return;
                }

                Player.Stop();
            }

            try
            {
                if (currentWannaSong.HasValue)
                {
                    Service.CurrentPlaylist.CurrentSong = currentWannaSong.Value.Song;
                    Player.Play(GetWaveProvider);
                }
                else
                {
                    Reader = null;
                    Service.AudioFormat = null;
                    Service.CurrentPlaylist.CurrentSong = null;
                }
            }
            catch
            {
                Reader = null;
                Service.AudioFormat = null;
            }
        }

        private IPositionWaveProvider GetWaveProvider()
        {
            currentReaderPath = currentWannaSong.Value.Song.FullPath;
            Reader = ToWaveProvider(CreateWaveProvider(currentWannaSong.Value.Song));

            if (Reader.TotalTime == currentWannaSong.Value.Duration && Reader.TotalTime > currentWannaSong.Value.Position)
            {
                Reader.CurrentTime = currentWannaSong.Value.Position;
            }
            
            Service.CurrentPlaylist.Position = Reader.CurrentTime;
            Service.CurrentPlaylist.Duration = Reader.TotalTime;

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
            if (!isSetCurrentSong && Service.CurrentPlaylist.CurrentSong.HasValue &&
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

            Player?.Dispose();

            Unsubscribe(Service);
        }
    }
}
