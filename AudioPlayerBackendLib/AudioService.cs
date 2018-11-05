using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using StdOttWpfLib;

namespace AudioPlayerBackendLib
{
    public class AudioService : AudioClient
    {
        private const int updateIntervall = 100;
        private static Random ran = new Random();

        private bool isUpdatingPosition;
        private readonly Timer timer;
        private readonly IntPtr? windowHandle;
        private readonly Player player;
        private AudioFileReader reader;

        public override IntPtr? WindowHandle { get { return windowHandle; } }

        public AudioService(IntPtr? windowHandle = null)
        {
            this.windowHandle = windowHandle;

            player = Player.GetPlayer(windowHandle);
            player.PlaybackStopped += Player_PlaybackStopped;

            timer = new Timer(updateIntervall);
            timer.Elapsed += Timer_Elapsed;

            Volume = player.Volume;
        }

        private static Dispatcher GetDefaultDispatcher()
        {
            if (Application.Current?.Dispatcher != null) return Application.Current.Dispatcher;

            return Dispatcher.CurrentDispatcher;
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception == null) SetNextSong();
        }

        private void Player_MediaEnded(object sender, EventArgs args)
        {
            SetNextSong();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            isUpdatingPosition = true;

            if (reader != null) Position = reader.CurrentTime;

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
            timer.Stop();

            Task.Factory.StartNew(SetCurrentSong);
        }

        private void SetCurrentSong()
        {
            if (reader != null) player.DisposeReader(reader);

            try
            {
                if (CurrentSong.HasValue)
                {
                    reader = new AudioFileReader(CurrentSong.Value.FullPath);
                    player.Init(ToWaveProvider(reader));

                    Duration = reader.TotalTime;
                }
                else
                {
                    reader = null;
                    Format = null;
                }
            }
            catch
            {
                reader = null;
                Format = null;
            }

            EnableTimer();
        }

        protected virtual IWaveProvider ToWaveProvider(IWaveProvider waveProvider)
        {
            return waveProvider;
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
                IEnumerable<string> nonHiddenFiles = sourcePaths.SelectMany(LoadFilePaths).Where(IsNotHidden);

                return nonHiddenFiles.Select(p => new Song(p));
            }
            catch
            {
                return Enumerable.Empty<Song>();
            }
        }

        private IEnumerable<string> LoadFilePaths(string path)
        {
            if (File.Exists(path)) yield return path;

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path)) yield return file;
            }
        }

        private bool IsNotHidden(string path)
        {
            FileInfo file = new FileInfo(path);

            return (file.Attributes & FileAttributes.Hidden) == 0;
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
            if (!isUpdatingPosition && reader != null) reader.CurrentTime = Position;
        }

        private void EnableTimer()
        {
            timer.Enabled = CurrentSong.HasValue && PlayState == PlaybackState.Playing;
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

            if (reader != null)
            {
                player.DisposeReader(reader);
                reader = null;
            }
        }
    }
}
