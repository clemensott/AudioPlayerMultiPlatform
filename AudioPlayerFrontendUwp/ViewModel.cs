using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend
{
    class ViewModel : IAudioExtended
    {
        private bool viewAdvancedSettings, syncPositionAndSlider;
        private double positionSliderPosition;

        public bool ViewAdvancedSettings
        {
            get { return viewAdvancedSettings; }
            set
            {
                if (value == viewAdvancedSettings) return;

                viewAdvancedSettings = value;
                OnPropertyChanged(nameof(ViewAdvancedSettings));
            }
        }

        public IAudioExtended Parent { get; private set; }

        public bool SyncPositionAndSlider
        {
            get { return syncPositionAndSlider; }
            set
            {
                if (value == syncPositionAndSlider) return;

                syncPositionAndSlider = value;
                OnPropertyChanged(nameof(SyncPositionAndSlider));

                Position = TimeSpan.FromSeconds(value);
            }
        }

        public double PositionSliderValue
        {
            get
            {
                if (SyncPositionAndSlider) positionSliderPosition = Position.TotalSeconds;

                return positionSliderPosition;
            }

            set
            {
                if (value == positionSliderPosition) return;

                positionSliderPosition = value;
                OnPropertyChanged(nameof(PositionSliderValue));

                if (SyncPositionAndSlider) Position = TimeSpan.FromSeconds(value);
            }
        }

        public TimeSpan Position
        {
            get { return Parent.Position; }
            set { Parent.Position = value; }
        }

        public TimeSpan Duration
        {
            get { return Parent.Duration; }
            set { Parent.Duration = value; }
        }

        public bool IsShuffle
        {
            get { return IsSearching ? IsSearchShuffle : IsAllShuffle; }
            set
            {
                if (IsSearching) IsSearchShuffle = value;
                else IsAllShuffle = value;
            }
        }

        public bool IsAllShuffle
        {
            get { return Parent.IsAllShuffle; }
            set { Parent.IsAllShuffle = value; }
        }

        public bool IsSearchShuffle
        {
            get { return Parent.IsSearchShuffle; }
            set { Parent.IsSearchShuffle = value; }
        }

        public bool IsOnlySearch
        {
            get { return Parent.IsOnlySearch; }
            set { Parent.IsOnlySearch = value; }
        }

        public bool IsSearching { get { return !string.IsNullOrEmpty(SearchKey); } }

        public string SearchKey
        {
            get { return Parent.SearchKey; }
            set { Parent.SearchKey = value; }
        }

        public PlaybackState PlayState
        {
            get { return Parent.PlayState; }
            set { Parent.PlayState = value; }
        }

        public string[] MediaSources
        {
            get { return Parent.MediaSources; }
            set { Parent.MediaSources = value; }
        }

        public Song? CurrentSong
        {
            get { return Parent.CurrentSong; }
            set { Parent.CurrentSong = value; }
        }

        public int CurrentViewSongIndex
        {
            get { return CurrentSong.HasValue ? ViewSongs.IndexOf(CurrentSong.Value) : -1; }
            set { if (value >= 0 && value < ViewSongs.Count()) CurrentSong = ViewSongs.ElementAt(value); }
        }

        public Song[] AllSongsShuffled
        {
            get { return Parent.AllSongsShuffled; }
            set { Parent.AllSongsShuffled = value; }
        }

        public IEnumerable<Song> AllSongs { get { return Parent.AllSongs; } }

        public IEnumerable<Song> SearchSongs { get { return Parent.SearchSongs; } }

        public IEnumerable<Song> ViewSongs { get { return Parent.IsSearching ? SearchSongs : AllSongs; } }

        public float Volume
        {
            get { return Parent.Volume; }
            set { Parent.Volume = value; }
        }

        public IPlayer Player { get { return Parent.Player; } }

        public ViewModel(IAudioExtended parent)
        {
            this.Parent = parent;
            parent.PropertyChanged += Parent_PropertyChanged;
        }

        public void SetNextSong()
        {
            Parent.SetNextSong();
        }

        public void SetPreviousSong()
        {
            Parent.SetPreviousSong();
        }

        public void Reload()
        {
            Parent.Reload();
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);

            switch (e.PropertyName)
            {
                case nameof(Position):
                    OnPropertyChanged(nameof(PositionSliderValue));
                    break;

                case nameof(IsAllShuffle):
                    if (!IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(IsSearchShuffle):
                    if (IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(SearchKey):
                    OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(AllSongs):
                    if (!IsSearching)
                    {
                        OnPropertyChanged(nameof(ViewSongs));
                        OnPropertyChanged(nameof(CurrentViewSongIndex));
                    }
                    break;

                case nameof(SearchSongs):
                    if (IsSearching)
                    {
                        OnPropertyChanged(nameof(ViewSongs));
                        OnPropertyChanged(nameof(CurrentViewSongIndex));
                    }
                    break;

                case nameof(CurrentSong):
                    OnPropertyChanged(nameof(CurrentViewSongIndex));
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            try
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                else await dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
            catch { }
        }

        public void Dispose()
        {
            Parent.Dispose();
        }
    }
}
