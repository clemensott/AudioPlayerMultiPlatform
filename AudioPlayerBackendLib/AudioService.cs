using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public class AudioService : AudioClient, IAudioService
    {
        private const int updateIntervall = 100;
        private static Random ran = new Random();

        private bool isUpdatingPosition;
        private IAudioServiceHelper helper;
        private readonly Timer timer;
        private readonly IPlayer player;
        private readonly object readerLockObj = new object();

        public IPositionWaveProvider Reader { get; set; }

        public override IPlayer Player { get { return player; } }

        public AudioService(IPlayer player, IAudioServiceHelper helper = null) : base(helper)
        {
            this.player = player;
            this.helper = helper;

            player.PlayState = PlayState;
            player.PlaybackStopped += Player_PlaybackStopped;

            timer = new Timer(Timer_Elapsed, null, updateIntervall, updateIntervall);

            Volume = player.Volume;
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception == null && (PlayState != PlaybackState.Stopped || Position >= Duration)) SetNextSong();
        }

        private void Player_MediaEnded(object sender, EventArgs args)
        {
            SetNextSong();
        }

        private void Timer_Elapsed(object state)
        {
            isUpdatingPosition = true;

            try
            {
                if (Reader != null) Position = Reader.CurrentTime;
            }
            catch { }

            isUpdatingPosition = false;
        }

        protected override void OnAllSongsShuffledChanged()
        {
            if (!AllSongsShuffled.Any()) CurrentSong = null;
            else if (!CurrentSong.HasValue || !AllSongsShuffled.Contains(CurrentSong.Value))
            {
                CurrentSong = AllSongsShuffled.FirstOrDefault();
            }
        }

        protected override void OnCurrentSongChanged()
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

            if (Reader != null) player.Stop(Reader);

            try
            {
                if (CurrentSong.HasValue)
                {
                    player.Play(GetWaveProvider);
                }
                else
                {
                    Reader = null;
                    Format = null;
                }
            }
            catch
            {
                Reader = null;
                Format = null;
            }
        }

        private IPositionWaveProvider GetWaveProvider()
        {
            Reader = ToWaveProvider(CreateWaveProvider(CurrentSong.Value));
            Duration = Reader.TotalTime;

            return Reader;
        }

        internal virtual IPositionWaveProvider ToWaveProvider(IPositionWaveProvider waveProvider)
        {
            return waveProvider;
        }

        protected virtual IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return helper.CreateWaveProvider(song, this);
        }

        protected override void OnMediaSourcesChanged()
        {
            Reload();
        }

        public override void Reload()
        {
            Song[] allSongsShuffled = GetShuffledSongs(LoadAllSongs()).ToArray();

            for (int i = 0; i < allSongsShuffled.Length; i++) allSongsShuffled[i].Index = i;

            AllSongsShuffled = allSongsShuffled;
        }

        private IEnumerable<Song> LoadAllSongs()
        {
            try
            {
                IEnumerable<string> sourcePaths = MediaSources.ToNotNull();
                IEnumerable<string> nonHiddenFiles = sourcePaths.SelectMany(LoadFilePaths);

                return nonHiddenFiles.Select(p => new Song(p));
            }
            catch
            {
                return Enumerable.Empty<Song>();
            }
        }

        protected virtual IEnumerable<string> LoadFilePaths(string path)
        {
            return helper.LoadFilePaths(path, this);
        }

        private IEnumerable<Song> GetShuffledSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => ran.Next());
        }

        protected override void OnPlayStateChanged()
        {
            player.PlayState = PlayState;

            EnableTimer();
        }

        protected override void OnPositionChanged()
        {
            if (!isUpdatingPosition && Reader != null)
            {
                Reader.CurrentTime = Position;
                Player.ExecutePlayState();
            }
        }

        private void EnableTimer()
        {
            if (CurrentSong.HasValue && PlayState == PlaybackState.Playing) StartTimer();
            else StopTimer();
        }

        private void StartTimer()
        {
            timer.Change(updateIntervall, updateIntervall);
        }


        private void StopTimer()
        {
            timer.Change(-1, -1);
        }

        protected override void OnServiceVolumeChanged()
        {
            player.Volume = Volume;
        }

        protected override void OnDurationChanged()
        {
        }

        protected override void OnIsAllShuffleChanged()
        {
        }

        protected override void OnIsSearchShuffleChanged()
        {
        }

        protected override void OnIsOnlySearchChanged()
        {
        }

        protected override void OnSearchKeyChanged()
        {
        }

        protected override void OnFormatChanged()
        {
        }

        protected override void OnAudioDataChanged()
        {
        }

        public override void Dispose()
        {
            player.PlaybackStopped -= Player_PlaybackStopped;

            if (Reader != null)
            {
                player.Stop(Reader);
                Reader = null;
            }
        }
    }
}
