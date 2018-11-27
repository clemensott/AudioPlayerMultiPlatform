using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace AudioPlayerFrontendWpf
{
    class ViewModel : IAudioExtended
    {
        public IAudioExtended Parent { get; private set; }

        public double PositionFactor
        {
            get { return Duration > TimeSpan.Zero ? Position.TotalDays / Duration.TotalDays : 0; }
            set { if (value != PositionFactor) Position = TimeSpan.FromDays(Duration.TotalDays * value); }
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

        public Song? CurrentViewSong
        {
            get { return CurrentSong.HasValue && ViewSongs.Contains(CurrentSong.Value) ? CurrentSong : null; }
            set { if (value.HasValue) CurrentSong = value.Value; }
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

        public IPlayer Player => throw new NotImplementedException();

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
                    OnPropertyChanged(nameof(PositionFactor));
                    break;

                case nameof(IsAllShuffle):
                    if (!IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(IsSearchShuffle):
                    if (IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(SearchKey):
                    OnPropertyChanged(nameof(IsShuffle));
                    OnPropertyChanged(nameof(CurrentViewSong));
                    break;

                case nameof(AllSongs):
                    if (!IsSearching) OnPropertyChanged(nameof(ViewSongs));
                    break;

                case nameof(SearchSongs):
                    if (IsSearching) OnPropertyChanged(nameof(ViewSongs));
                    break;

                case nameof(CurrentSong):
                    OnPropertyChanged(nameof(CurrentViewSong));
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            catch { }
        }

        public void Dispose()
        {
            Parent.Dispose();
        }
    }
}
