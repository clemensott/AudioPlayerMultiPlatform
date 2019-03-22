using AudioPlayerBackend.Player;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerBackend.Audio
{
    public class AudioService : IAudioService
    {
        public event EventHandler<ValueChangedEventArgs<IPlaylistBase>> CurrentPlaylistChanged;
        public event EventHandler<ValueChangedEventArgs<IPlaylistBase[]>> PlaylistsChanged;
        public event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;
        public event EventHandler<ValueChangedEventArgs<WaveFormat>> AudioFormatChanged;
        public event EventHandler<ValueChangedEventArgs<byte[]>> AudioDataChanged;

        private readonly INotifyPropertyChangedHelper helper;
        private PlaybackState playState;
        private IPlaylist currentPlaylist;
        private IPlaylist[] playlists;
        private WaveFormat audioFormat;
        private byte[] audioData;
        private float volume;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                if (value == playState) return;

                var args = new ValueChangedEventArgs<PlaybackState>(PlayState, value);
                playState = value;
                PlayStateChanged?.Invoke(this, args);

                OnPlayStateChanged();
                OnPropertyChanged(nameof(PlayState));
            }
        }

        public ISourcePlaylist SourcePlaylist { get; private set; }

        public IPlaylist CurrentPlaylist
        {
            get => currentPlaylist;
            set
            {
                if (value == null) value = SourcePlaylist;

                if (value == currentPlaylist) return;

                var args = new ValueChangedEventArgs<IPlaylistBase>(CurrentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);

                OnCurrentPlaylistChanged();
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public IPlaylist[] Playlists
        {
            get => playlists;
            set
            {
                if (value == playlists) return;

                var args = new ValueChangedEventArgs<IPlaylistBase[]>(Playlists, value);
                playlists = value;
                PlaylistsChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(Playlists));
            }
        }

        public WaveFormat AudioFormat
        {
            get => audioFormat;
            set
            {
                if (value == audioFormat) return;

                var args = new ValueChangedEventArgs<WaveFormat>(AudioFormat, value);
                audioFormat = value;
                AudioFormatChanged?.Invoke(this, args);

                OnFormatChanged();
                OnPropertyChanged(nameof(AudioFormat));
            }
        }

        public byte[] AudioData
        {
            get => audioData;
            set
            {
                if (value.BothNullOrSequenceEqual(audioData)) return;

                var args = new ValueChangedEventArgs<byte[]>(AudioData, value);
                audioData = value;
                AudioDataChanged?.Invoke(this, args);

                OnAudioDataChanged();
                OnPropertyChanged(nameof(AudioData));
            }
        }

        public float Volume
        {
            get => volume;
            set
            {
                if (value == volume) return;

                var args = new ValueChangedEventArgs<float>(Volume, value);
                volume = value;
                VolumeChanged?.Invoke(this, args);

                OnServiceVolumeChanged();
                OnPropertyChanged(nameof(Volume));
            }
        }

        ISourcePlaylistBase IAudioServiceBase.SourcePlaylist => SourcePlaylist;

        IPlaylistBase IAudioServiceBase.CurrentPlaylist { get => CurrentPlaylist; set => CurrentPlaylist = (IPlaylist)value; }

        IPlaylistBase[] IAudioServiceBase.Playlists { get => Playlists; set => Playlists = value.Cast<IPlaylist>().ToArray(); }

        public AudioService(IAudioServiceHelper helper = null)
        {
            this.helper = helper;
            playState = PlaybackState.Stopped;

            Playlists = new IPlaylist[0];
            CurrentPlaylist = SourcePlaylist = Audio.SourcePlaylist.GetInstance(helper);
        }

        protected virtual void OnPlayStateChanged() { }

        protected virtual void OnCurrentPlaylistChanged() { }

        protected virtual void OnFormatChanged() { }

        protected virtual void OnAudioDataChanged() { }

        protected virtual void OnServiceVolumeChanged() { }

        public void Continue()
        {
            if (CurrentPlaylist.Loop == LoopType.CurrentSong)
            {
                CurrentPlaylist.Position = TimeSpan.Zero;
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsService.GetNextSong(CurrentPlaylist);

            if (CurrentPlaylist.Loop == LoopType.CurrentPlaylist || !overflow)
            {
                ChangeCurrentSongOrRestart(CurrentPlaylist, newCurrentSong);
            }
            else if (CurrentPlaylist.Loop == LoopType.Next)
            {
                CurrentPlaylist = GetAllPlaylists().Next(CurrentPlaylist).next;
            }
            else if (CurrentPlaylist.Loop == LoopType.Stop)
            {
                CurrentPlaylist = GetAllPlaylists().Next(CurrentPlaylist).next;
                PlayState = PlaybackState.Stopped;
            }
        }

        public IEnumerable<IPlaylist> GetAllPlaylists()
        {
            foreach (IPlaylist playlist in Playlists) yield return playlist;

            yield return SourcePlaylist;
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetNextSong(CurrentPlaylist).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetPreviousSong(CurrentPlaylist).song);
        }

        private static void ChangeCurrentSongOrRestart(IPlaylistBase playlist, Song? newCurrentSong)
        {
            if (newCurrentSong != playlist.CurrentSong) playlist.CurrentSong = newCurrentSong;
            else playlist.Position = TimeSpan.Zero;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            if (helper?.InvokeDispatcher != null) helper.InvokeDispatcher(Raise);
            else Raise();

            void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
