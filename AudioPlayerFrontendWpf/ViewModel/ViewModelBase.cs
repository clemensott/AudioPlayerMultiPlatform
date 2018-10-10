using AudioPlayerBackendLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerFrontendWpf.ViewModel
{
    abstract class ViewModelBase : INotifyPropertyChanged
    {
        private const string folderPathFileName = "path.txt";

        private Song[] songs;

        public double PositionFactor
        {
            get { return Duration > TimeSpan.Zero ? Position.TotalDays / Duration.TotalDays : 0; }
            set
            {
                if (value == PositionFactor) return;

                Position = TimeSpan.FromDays(Duration.TotalDays * value);

                OnNotifyPropertyChanged("PositionFactor");
            }
        }

        public abstract TimeSpan Position { get; set; }

        public abstract TimeSpan Duration { get; protected set; }

        public abstract PlayState PlayState { get; set; }

        public abstract bool IsAllShuffle { get; set; }

        public abstract bool IsSearchShuffle { get; set; }

        public bool IsShuffle
        {
            get { return IsSearching ? IsSearchShuffle : IsAllShuffle; }
            set
            {
                if (IsSearching) IsSearchShuffle = value;
                else IsAllShuffle = value;
            }
        }

        public abstract bool IsOnlySearch { get; set; }

        public bool IsSearching { get { return !string.IsNullOrEmpty(SearchKey); } }

        public abstract string SearchKey { get; set; }

        public Song? CurrentViewSong
        {
            get { return Songs.Contains(CurrentPlaySong) ? (Song?)CurrentPlaySong : null; }
            set { if (value.HasValue) CurrentPlaySong = value.Value; }
        }

        public abstract Song CurrentPlaySong { get; set; }

        public abstract string[] Sources { get; set; }

        public Song[] Songs
        {
            get { return songs; }
            protected set
            {
                if (value == songs) return;

                songs = value;

                OnNotifyPropertyChanged(nameof(Songs));
                OnNotifyPropertyChanged(nameof(CurrentViewSong));
            }
        }

        public ViewModelBase()
        {
            songs = new Song[0];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract void SetPreviousSong();

        public abstract void SetNextSong();

        public abstract void Refresh();
    }
}
