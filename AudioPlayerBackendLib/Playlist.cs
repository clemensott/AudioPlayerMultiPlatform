using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public class Playlist : IPlaylistExtended, IEquatable<Playlist>
    {
        protected bool isAllShuffle, isSearchShuffle, isOnlySearch;
        protected string searchKey;
        protected TimeSpan position, duration;
        protected LoopType loop;
        protected Song? currentSong;
        protected Song[] songs;

        public IEnumerable<Song> AllSongs => SongsService.GetAllSongs(this);

        public IEnumerable<Song> SearchSongs => SongsService.GetSearchSongs(this);

        public Guid ID { get; set; }

        public bool IsAllShuffle
        {
            get { return isAllShuffle; }
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;

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

                OnPropertyChanged(nameof(SearchKey));
                OnPropertyChanged(nameof(IsSearching));

                if (IsSearching) OnPropertyChanged(nameof(SearchSongs));
                else OnPropertyChanged(nameof(AllSongs));
            }
        }

        public LoopType Loop
        {
            get { return loop; }
            set
            {
                if (value == loop) return;

                loop = value;
                OnPropertyChanged(nameof(Loop));
            }
        }

        public TimeSpan Position
        {
            get { return position; }
            set
            {
                if (value == position) return;

                position = value;
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
                OnPropertyChanged(nameof(Duration));
            }
        }

        public Song? CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong) return;

                currentSong = value;
                OnPropertyChanged(nameof(CurrentSong));
            }
        }

        public Song[] Songs
        {
            get { return songs; }
            set
            {
                if (value == songs) return;

                songs = value;

                OnPropertyChanged(nameof(Songs));
                OnPropertyChanged(nameof(AllSongs));

                if (IsSearching) OnPropertyChanged(nameof(SearchSongs));
            }
        }

        public Playlist()
        {
            ID = Guid.NewGuid();
            Loop = LoopType.CurrentPlaylist;
            Songs = new Song[0];
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(SongsService.GetNextSong(this).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(SongsService.GetPreviousSong(this).song);
        }

        private void ChangeCurrentSongOrRestart(Song? newCurrentSong)
        {
            if (newCurrentSong != CurrentSong) CurrentSong = newCurrentSong;
            else Position = TimeSpan.Zero;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Playlist);
        }

        public bool Equals(Playlist other)
        {
            return other != null &&
                   ID.Equals(other.ID) &&
                   IsAllShuffle == other.IsAllShuffle &&
                   IsSearchShuffle == other.IsSearchShuffle &&
                   IsOnlySearch == other.IsOnlySearch &&
                   IsSearching == other.IsSearching &&
                   SearchKey == other.SearchKey &&
                   Loop == other.Loop &&
                   Position.Equals(other.Position) &&
                   Duration.Equals(other.Duration) &&
                   EqualityComparer<Song?>.Default.Equals(CurrentSong, other.CurrentSong) &&
                   EqualityComparer<Song[]>.Default.Equals(Songs, other.Songs);
        }

        public override int GetHashCode()
        {
            var hashCode = -1043069822;
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<Song>>.Default.GetHashCode(AllSongs);
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<Song>>.Default.GetHashCode(SearchSongs);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(ID);
            hashCode = hashCode * -1521134295 + IsAllShuffle.GetHashCode();
            hashCode = hashCode * -1521134295 + IsSearchShuffle.GetHashCode();
            hashCode = hashCode * -1521134295 + IsOnlySearch.GetHashCode();
            hashCode = hashCode * -1521134295 + IsSearching.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SearchKey);
            hashCode = hashCode * -1521134295 + Loop.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan>.Default.GetHashCode(Duration);
            hashCode = hashCode * -1521134295 + EqualityComparer<Song?>.Default.GetHashCode(CurrentSong);
            hashCode = hashCode * -1521134295 + EqualityComparer<Song[]>.Default.GetHashCode(Songs);
            return hashCode;
        }

        public static bool operator ==(Playlist playlist1, IPlaylist playlist2)
        {
            return EqualityComparer<IPlaylist>.Default.Equals(playlist1, playlist2);
        }

        public static bool operator !=(Playlist playlist1, IPlaylist playlist2)
        {
            return !(playlist1 == playlist2);
        }
    }
}
