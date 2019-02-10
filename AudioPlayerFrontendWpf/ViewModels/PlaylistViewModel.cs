using AudioPlayerBackend;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerFrontend
{
    class PlaylistViewModel : IPlaylistExtended
    {
        private IPlaylistExtended @base;

        public IPlaylistExtended Base
        {
            get => @base;
            private set
            {
                if (value == @base) return;

                if (@base != null) @base.PropertyChanged -= Base_PropertyChanged;
                @base = value;
                if (@base != null) @base.PropertyChanged += Base_PropertyChanged;
            }
        }

        public IEnumerable<Song> AllSongs { get => Base.AllSongs; }

        public IEnumerable<Song> SearchSongs { get => Base.SearchSongs; }

        public IEnumerable<Song> ViewSongs => IsSearching ? SearchSongs : AllSongs;

        public Guid ID { get => Base.ID; set => Base.ID = value; }

        public bool IsAllShuffle { get => Base.IsAllShuffle; set => Base.IsAllShuffle = value; }

        public bool IsSearchShuffle { get => Base.IsSearchShuffle; set => Base.IsSearchShuffle = value; }

        public bool IsShuffle
        {
            get => IsSearching ? IsSearchShuffle : IsAllShuffle;
            set
            {
                if (IsSearching) IsSearchShuffle = value;
                else IsAllShuffle = value;
            }
        }

        public bool IsOnlySearch { get => Base.IsOnlySearch; set => Base.IsOnlySearch = value; }

        public bool IsSearching { get => Base.IsSearching; }

        public string SearchKey { get => Base.SearchKey; set => Base.SearchKey = value; }

        public LoopType Loop { get => Base.Loop; set => Base.Loop = value; }

        public TimeSpan Position { get => Base.Position; set => Base.Position = value; }

        public TimeSpan Duration { get => Base.Duration; set => Base.Duration = value; }

        public Song? CurrentSong { get => Base.CurrentSong; set => Base.CurrentSong = value; }

        public int CurrentViewSongIndex
        {
            get => Base.CurrentSong.HasValue ? ViewSongs.IndexOf(Base.CurrentSong.Value) : -1;
            set
            {
                if (value != -1) Base.CurrentSong = ViewSongs.ElementAt(value);
            }
        }

        public Song[] Songs { get => Base.Songs; set => Base.Songs = value; }

        public PlaylistViewModel(IPlaylistExtended @base)
        {
            Base = @base;
        }

        private void Base_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);

            switch (e.PropertyName)
            {
                case nameof(IsAllShuffle):
                    if (!IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(IsSearchShuffle):
                    if (IsSearching) OnPropertyChanged(nameof(IsShuffle));
                    break;

                case nameof(IsSearching):
                    if (IsAllShuffle ^ IsSearchShuffle) OnPropertyChanged(nameof(IsShuffle));
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

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
