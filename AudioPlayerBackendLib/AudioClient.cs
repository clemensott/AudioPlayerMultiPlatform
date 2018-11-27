using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public abstract class AudioClient : IAudioExtended
    {
        protected TimeSpan position, duration;
        protected PlaybackState playState;
        protected bool isAllShuffle, isSearchShuffle, isOnlySearch;
        protected string searchKey;
        protected string[] mediaSources;
        protected Song? currentSong;
        protected Song[] allSongsShuffled;
        protected WaveFormat format;
        protected byte[] audioData;
        protected float serviceVolume;

        public TimeSpan Position
        {
            get { return position; }
            set
            {
                if (value == position) return;

                position = value;
                OnPositionChanged();
                OnPropertyChanged(nameof(Position));
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set
            {
                if (value == duration) return;

                duration = value;
                OnDurationChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

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

        public bool IsAllShuffle
        {
            get { return isAllShuffle; }
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;

                OnIsAllShuffleChanged();
                OnPropertyChanged(nameof(IsAllShuffle));
                OnPropertyChanged(nameof(AllSongs));
            }
        }

        public bool IsSearchShuffle
        {
            get { return isSearchShuffle; }
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;

                OnIsSearchShuffleChanged();
                OnPropertyChanged(nameof(IsSearchShuffle));

                if (IsSearching) OnPropertyChanged(nameof(SearchSongs));
            }
        }

        public bool IsOnlySearch
        {
            get { return isOnlySearch; }
            set
            {
                if (value == isOnlySearch) return;

                isOnlySearch = value;
                OnIsOnlySearchChanged();
                OnPropertyChanged(nameof(IsOnlySearch));
            }
        }

        public bool IsSearching { get { return SongsService.GetIsSearching(SearchKey); } }

        public string SearchKey
        {
            get { return searchKey; }
            set
            {
                if (value == searchKey) return;

                searchKey = value;

                OnSearchKeyChanged();
                OnPropertyChanged(nameof(SearchKey));
                OnPropertyChanged(nameof(IsSearching));

                if (IsSearching) OnPropertyChanged(nameof(SearchSongs));
                else OnPropertyChanged(nameof(AllSongs));
            }
        }

        public string[] MediaSources
        {
            get { return mediaSources; }
            set
            {
                if (value.BothNullOrSequenceEqual(mediaSources)) return;

                mediaSources = value;
                OnMediaSourcesChanged();
                OnPropertyChanged(nameof(MediaSources));
            }
        }

        public Song? CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong) return;

                currentSong = value;
                OnCurrentSongChanged();
                OnPropertyChanged(nameof(CurrentSong));
            }
        }

        public Song[] AllSongsShuffled
        {
            get { return allSongsShuffled; }
            set
            {
                if (value.BothNullOrSequenceEqual(allSongsShuffled)) return;

                allSongsShuffled = value;

                OnAllSongsShuffledChanged();

                OnPropertyChanged(nameof(AllSongsShuffled));
                OnPropertyChanged(nameof(AllSongs));

                if (IsSearching) OnPropertyChanged(nameof(SearchSongs));
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

        public IEnumerable<Song> AllSongs { get { return SongsService.GetAllSongs(this); } }

        public IEnumerable<Song> SearchSongs { get { return SongsService.GetSearchSongs(this); } }

        public abstract IPlayer Player { get; }

        public AudioClient()
        {
            playState = PlaybackState.Stopped;
            mediaSources = new string[0];
            allSongsShuffled = new Song[0];
        }

        protected abstract void OnPositionChanged();

        protected abstract void OnDurationChanged();

        protected abstract void OnPlayStateChanged();

        protected abstract void OnIsAllShuffleChanged();

        protected abstract void OnIsSearchShuffleChanged();

        protected abstract void OnIsOnlySearchChanged();

        protected abstract void OnSearchKeyChanged();

        protected abstract void OnMediaSourcesChanged();

        protected abstract void OnCurrentSongChanged();

        protected abstract void OnAllSongsShuffledChanged();

        protected abstract void OnFormatChanged();

        protected abstract void OnAudioDataChanged();

        protected abstract void OnServiceVolumeChanged();

        public void SetNextSong()
        {
            CurrentSong = SongsService.GetNextSong(this);
        }

        public void SetPreviousSong()
        {
            CurrentSong = SongsService.GetPreviousSong(this);
        }

        public abstract void Reload();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void Dispose();
    }
}
