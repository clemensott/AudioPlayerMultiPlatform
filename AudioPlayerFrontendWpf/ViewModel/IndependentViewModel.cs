using AudioPlayerBackendLib;
using System;
using System.Linq;
using System.Windows.Threading;

namespace AudioPlayerFrontendWpf.ViewModel
{
    class IndependentViewModel : ViewModelBase
    {
        private readonly AudioServiceReference.AudioServiceClient service;
        private readonly DispatcherTimer timer;

        private Hashes hashes;
        private TimeSpan position, duration;
        private PlayState playState;
        private bool isAllShuffle, isSearchShuffle, isOnlySearch;
        private string searchKey;
        private string[] sources;
        private Song currentSong;

        public override TimeSpan Position
        {
            get { return position; }
            set
            {
                if (value == position) return;

                position = value;
                service.SetPosition(value);

                OnNotifyPropertyChanged(nameof(Position));
            }
        }

        public override TimeSpan Duration
        {
            get { return duration; }
            protected set
            {
                if (value == duration) return;

                duration = value;
                OnNotifyPropertyChanged(nameof(Duration));
            }
        }

        public override PlayState PlayState
        {
            get { return playState; }
            set
            {
                if (value == playState) return;

                playState = value;
                service.SetPlayState(Convert(value));

                OnNotifyPropertyChanged(nameof(PlayState));
            }
        }

        public override bool IsAllShuffle
        {
            get { return isAllShuffle; }
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;
                service.SetIsAllShuffle(value);

                OnNotifyPropertyChanged(nameof(IsAllShuffle));
                OnNotifyPropertyChanged(nameof(IsShuffle));
            }
        }

        public override bool IsSearchShuffle
        {
            get { return isSearchShuffle; }
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;
                service.SetIsSearchShuffle(value);

                OnNotifyPropertyChanged(nameof(IsSearchShuffle));
                OnNotifyPropertyChanged(nameof(IsShuffle));
            }
        }

        public override bool IsOnlySearch
        {
            get { return isOnlySearch; }
            set
            {
                if (value == isOnlySearch) return;

                isOnlySearch = value;
                service.SetIsOnlySearch(value);

                OnNotifyPropertyChanged(nameof(IsOnlySearch));
            }
        }

        public override string SearchKey
        {
            get { return searchKey; }
            set
            {
                if (value == searchKey) return;

                if (value.Length > 0 && searchKey.Length == 0) Songs = service.GetAllSongs().Select(Convert).ToArray();
                else if (value.Length > 0 && searchKey.Length == 0) Songs = service.GetSearchSongs().Select(Convert).ToArray();

                searchKey = value;
                service.SetSearchKey(value);

                OnNotifyPropertyChanged(nameof(SearchKey));
                OnNotifyPropertyChanged(nameof(IsSearching));
                OnNotifyPropertyChanged(nameof(IsShuffle));
            }
        }

        public override Song CurrentPlaySong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong) return;

                currentSong = value;
                service.SetCurrentSong(Convert(value));

                OnNotifyPropertyChanged(nameof(CurrentViewSong));
                OnNotifyPropertyChanged(nameof(CurrentPlaySong));
            }
        }

        public override string[] Sources
        {
            get { return sources; }
            set
            {
                if (value == sources) return;

                sources = value;
                service.SetMediaSources(value);

                OnNotifyPropertyChanged(nameof(Sources));
            }
        }

        public IndependentViewModel()
        {
            service = new AudioServiceReference.AudioServiceClient();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            States states = Convert(service.GetStates());
            Hashes hashes = states.Hashes;

            Position = states.Position;
            Duration = states.Duration;
            PlayState = states.PlayState;
            IsAllShuffle = states.IsAllShuffle;
            IsSearchShuffle = states.IsSearchShuffle;
            IsOnlySearch = states.IsOnlySearch;
            SearchKey = states.SearchKey;

            if (hashes.MediaSourcesHash != this.hashes.MediaSourcesHash) Sources = service.GetMediaSources();
            if (hashes.CurrentSongHash != this.hashes.CurrentSongHash) CurrentPlaySong = Convert(service.GetCurrentSong());
            if (!IsSearching && hashes.AllSongsHash != this.hashes.AllSongsHash) Songs = service.GetAllSongs().Select(Convert).ToArray();
            if (!IsSearching && hashes.SearchSongsHash != this.hashes.SearchSongsHash) Songs = service.GetSearchSongs().Select(Convert).ToArray();

            this.hashes = hashes;
        }

        private static Song Convert(AudioServiceReference.Song song)
        {
            return new Song(song.Index, song.Title, song.Artist, song.FullPath);
        }

        private static AudioServiceReference.Song Convert(Song song)
        {
            return new AudioServiceReference.Song()
            {
                Index = song.Index,
                Title = song.Title,
                Artist = song.Artist,
                FullPath = song.FullPath
            };
        }

        public override void SetPreviousSong()
        {
            service.Previous();
        }

        public override void SetNextSong()
        {
            service.Next();
        }

        public override void Refresh()
        {
            //service.Refesh();
        }

        private static States Convert(AudioServiceReference.States s)
        {
            PlayState playState = Convert(s.PlayState);
            Hashes hashes = Convert(s.Hashes);

            return new States(s.Position, s.Duration, playState, s.IsAllShuffle,
                s.IsSearchShuffle, s.IsOnlySearch, s.SearchKey, hashes);
        }

        private static Hashes Convert(AudioServiceReference.Hashes h)
        {
            return new Hashes(h.MediaSourcesHash, h.CurrentSongHash, h.AllSongsHash, h.SearchSongsHash);
        }

        private static PlayState Convert(string state)
        {
            return (PlayState)Enum.Parse(typeof(PlayState), state);
        }

        private static string Convert(PlayState state)
        {
            return Enum.GetName(typeof(PlayState), state);
        }
    }
}
