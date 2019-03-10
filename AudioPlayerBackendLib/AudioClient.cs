using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerBackend
{
    public abstract class AudioClient : IAudioExtended
    {
        private readonly IAudioClientHelper helper;
        protected PlaybackState playState;
        protected IPlaylistExtended currentPlaylist;
        protected string[] mediaSources;
        protected WaveFormat format;
        protected byte[] audioData;
        protected float serviceVolume;

        public PlaybackState PlayState
        {
            get { return playState; }
            set
            {
                if (value == playState) return;

                playState = value;
                OnPlayStateChanged();
                OnPropertyChanged(nameof(PlayState));
            }
        }

        public IPlaylistExtended FileBasePlaylist { get; private set; }

        public IPlaylistExtended CurrentPlaylist
        {
            get { return currentPlaylist; }
            set
            {
                if (value == null) value = FileBasePlaylist;

                if (value == currentPlaylist) return;

                currentPlaylist = value;
                OnCurrenPlaylistChanged();
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public ObservableCollection<IPlaylistExtended> AdditionalPlaylists { get; private set; }

        public string[] FileMediaSources
        {
            get { return mediaSources; }
            set
            {
                if (value.BothNullOrSequenceEqual(mediaSources)) return;

                mediaSources = value;
                OnMediaSourcesChanged();
                OnPropertyChanged(nameof(FileMediaSources));
            }
        }

        public WaveFormat Format
        {
            get { return format; }
            set
            {
                if (value == format) return;

                format = value;
                OnFormatChanged();
                OnPropertyChanged(nameof(Format));
            }
        }

        public byte[] AudioData
        {
            get { return audioData; }
            set
            {
                if (value.BothNullOrSequenceEqual(audioData)) return;

                audioData = value;
                OnAudioDataChanged();
                OnPropertyChanged(nameof(AudioData));
            }
        }

        public float Volume
        {
            get { return serviceVolume; }
            set
            {
                if (value == serviceVolume) return;

                serviceVolume = value;
                OnServiceVolumeChanged();
                OnPropertyChanged(nameof(Volume));
            }
        }

        public abstract IPlayer Player { get; }

        public AudioClient(IAudioClientHelper helper = null)
        {
            this.helper = helper;

            playState = PlaybackState.Stopped;
            mediaSources = new string[0];

            AdditionalPlaylists = new ObservableCollection<IPlaylistExtended>();
            AdditionalPlaylists.CollectionChanged += AdditionalPlaylists_CollectionChanged;

            CurrentPlaylist = FileBasePlaylist = new Playlist() { ID = new Guid() };
            FileBasePlaylist.PropertyChanged += Playlist_PropertyChanged;
        }

        private void AdditionalPlaylists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (IPlaylist playlist in (IEnumerable)e.OldItems ?? Enumerable.Empty<IPlaylist>())
            {
                playlist.PropertyChanged -= Playlist_PropertyChanged;

                if (CurrentPlaylist == playlist) CurrentPlaylist = GetAllPlaylists().ElementAt(e.OldStartingIndex);

                OnRemovePlaylist(playlist);
            }

            foreach (IPlaylist playlist in (IEnumerable)e.NewItems ?? Enumerable.Empty<IPlaylist>())
            {
                playlist.PropertyChanged += Playlist_PropertyChanged;

                OnAddPlaylist(playlist);
            }
        }

        private void Playlist_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IPlaylist playlist = (IPlaylist)sender;

            switch (e.PropertyName)
            {
                case nameof(playlist.CurrentSong):
                    OnCurrentSongChanged(playlist);
                    break;

                case nameof(playlist.Duration):
                    OnDurationChanged(playlist);
                    break;

                case nameof(playlist.IsAllShuffle):
                    OnIsAllShuffleChanged(playlist);
                    break;

                case nameof(playlist.IsOnlySearch):
                    OnIsOnlySearchChanged(playlist);
                    break;

                case nameof(playlist.IsSearchShuffle):
                    OnIsSearchShuffleChangedAsync(playlist);
                    break;

                case nameof(playlist.Loop):
                    OnLoopChanged(playlist);
                    break;

                case nameof(playlist.Position):
                    OnPositionChanged(playlist);
                    break;

                case nameof(playlist.SearchKey):
                    OnSearchKeyChanged(playlist);
                    break;

                case nameof(playlist.Songs):
                    OnSongsChanged(playlist);
                    break;
            }
        }

        protected virtual void OnPlayStateChanged() { }

        protected virtual void OnCurrenPlaylistChanged() { }

        protected virtual void OnMediaSourcesChanged() { }

        protected virtual void OnFormatChanged() { }

        protected virtual void OnAudioDataChanged() { }

        protected virtual void OnServiceVolumeChanged() { }

        protected virtual void OnCurrentSongChanged(IPlaylist playlist) { }

        protected virtual void OnDurationChanged(IPlaylist playlist) { }

        protected virtual void OnIsAllShuffleChanged(IPlaylist playlist) { }

        protected virtual void OnIsOnlySearchChanged(IPlaylist playlist) { }

        protected virtual void OnIsSearchShuffleChangedAsync(IPlaylist playlist) { }

        protected virtual void OnLoopChanged(IPlaylist playlist) { }

        protected virtual void OnPositionChanged(IPlaylist playlist) { }

        protected virtual void OnSearchKeyChanged(IPlaylist playlist) { }

        protected virtual void OnSongsChanged(IPlaylist playlist) { }

        protected virtual void OnAddPlaylist(IPlaylist playlist) { }

        protected virtual void OnRemovePlaylist(IPlaylist playlist) { }

        protected void Continue()
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

        public IEnumerable<IPlaylistExtended> GetAllPlaylists()
        {
            foreach (IPlaylistExtended playlist in AdditionalPlaylists) yield return playlist;

            yield return FileBasePlaylist;
        }

        public IPlaylistExtended GetPlaylist(Guid id)
        {
            return GetAllPlaylists().First(p => p.ID == id);
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetNextSong(CurrentPlaylist).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetPreviousSong(CurrentPlaylist).song);
        }

        private void ChangeCurrentSongOrRestart(IPlaylist playlist, Song? newCurrentSong)
        {
            if (newCurrentSong != playlist.CurrentSong) playlist.CurrentSong = newCurrentSong;
            else playlist.Position = TimeSpan.Zero;
        }

        public abstract void Reload();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            InvokeDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        protected virtual void InvokeDispatcher(Action action)
        {
            if (helper.InvokeDispatcher != null) helper.InvokeDispatcher(action);
            else action();
        }

        public abstract void Dispose();
    }
}
