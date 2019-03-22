using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public class AudioServicePlayer : IServicePlayer
    {
        private const int updateInterval = 100;
        private static readonly Random ran = new Random();

        private bool isUpdatingPosition;
        private readonly IAudioServicePlayerHelper helper;
        private readonly Timer timer;
        private readonly object readerLockObj = new object();
        private ReadEventWaveProvider waveProvider;

        public IPositionWaveProvider Reader { get; private set; }

        public IAudioService Service { get; }

        public IWaveProviderPlayer Player { get; }

        public AudioServicePlayer(IAudioService service, IWaveProviderPlayer player, IAudioServicePlayerHelper helper = null)
        {
            Service = service;
            Player = player;
            this.helper = helper;

            player.PlayState = service.PlayState;
            player.PlaybackStopped += Player_PlaybackStopped;

            timer = new Timer(Timer_Elapsed, null, updateInterval, updateInterval);

            service.Volume = player.Volume;

            Subscribe(Service);
            CheckCurrentSong(Service.CurrentPlaylist);
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
                    isUpdatingPosition = true;
                    Service.CurrentPlaylist.Position = Reader.CurrentTime;
                    isUpdatingPosition = false;
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

        private void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += Playlist_CurrentSongChanged;
            playlist.PositionChanged += Playlist_PositionChanged;
            playlist.SongsChanged += Playlist_SongsChanged;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= Playlist_CurrentSongChanged;
            playlist.PositionChanged -= Playlist_PositionChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;
        }

        private void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            CheckCurrentSong(Service.CurrentPlaylist);
            UpdateCurrentSong();
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

        private void Playlist_CurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            UpdateCurrentSong();
        }

        private void Playlist_PositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            if (!isUpdatingPosition && Reader != null)
            {
                Reader.CurrentTime = Service.CurrentPlaylist.Position;
                Player.ExecutePlayState();
            }
        }

        private void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            CheckCurrentSong((IPlaylistBase)sender);
        }

        private static void CheckCurrentSong(IPlaylistBase playlist)
        {
            if (playlist.Songs == null || playlist.Songs.Length == 0) playlist.CurrentSong = null;
            else if (!playlist.CurrentSong.HasValue || !playlist.Songs.Contains(playlist.CurrentSong.Value))
            {
                playlist.CurrentSong = playlist.Songs.First();
            }
        }

        private void UpdateCurrentSong()
        {
            StopTimer();

            Task.Factory.StartNew(SetCurrentSong);
        }

        private void SetCurrentSong()
        {
            lock (readerLockObj)
            {
                SetCurrentSongThreadSafe();
            }

            EnableTimer();
        }

        protected virtual void SetCurrentSongThreadSafe()
        {
            if (helper?.SetCurrentSongThreadSafe != null)
            {
                helper.SetCurrentSongThreadSafe(this);
                return;
            }

            if (Reader != null) Player.Stop();

            try
            {
                if (Service.CurrentPlaylist.CurrentSong.HasValue)
                {
                    Player.Play(GetWaveProvider);
                }
                else
                {
                    Reader = null;
                    Service.AudioFormat = null;
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
            Reader = ToWaveProvider(CreateWaveProvider(Service.CurrentPlaylist.CurrentSong.Value));

            if (Reader.TotalTime == Service.CurrentPlaylist.Duration && Reader.TotalTime > Service.CurrentPlaylist.Position)
            {
                Reader.CurrentTime = Service.CurrentPlaylist.Position;
            }
            else
            {
                if (!isUpdatingPosition)
                {
                    isUpdatingPosition = true;
                    Service.CurrentPlaylist.Position = Reader.CurrentTime;
                    isUpdatingPosition = false;
                }

                Service.CurrentPlaylist.Duration = Reader.TotalTime;
            }

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

        private IEnumerable<Song> GetShuffledSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => ran.Next());
        }

        private void EnableTimer()
        {
            if (Service.CurrentPlaylist.CurrentSong.HasValue &&
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

            if (Reader != null)
            {
                Player.Stop();
                Reader = null;
            }
        }
    }
}
