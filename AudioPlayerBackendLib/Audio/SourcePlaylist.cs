using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
        private readonly IAudioServiceHelper helper;
        private bool isSearchShuffle, isSearching;
        private string searchKey;
        private string[] fileMediaSources;
        private IEnumerable<Song> shuffledSongs;
        private IEnumerable<Song> searchSongs;

        public event EventHandler<ValueChangedEventArgs<bool>> IsSearchShuffleChanged;
        public event EventHandler<ValueChangedEventArgs<string>> SearchKeyChanged;
        public event EventHandler<ValueChangedEventArgs<string[]>> FileMediaSourcesChanged;

        public bool IsSearchShuffle
        {
            get => isSearchShuffle;
            set
            {
                if (value == isSearchShuffle) return;

                var args = new ValueChangedEventArgs<bool>(IsSearchShuffle, value);
                isSearchShuffle = value;
                IsSearchShuffleChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(IsSearchShuffle));

                if (IsSearchShuffle) ShuffledSongs = SongsService.GetShuffledSongs(Songs).ToBuffer();
                else SearchSongs = SongsService.GetSearchSongs(this).ToBuffer();
            }
        }

        public string SearchKey
        {
            get => searchKey;
            set
            {
                if (value == searchKey) return;

                var args = new ValueChangedEventArgs<string>(SearchKey, value);
                searchKey = value;
                SearchKeyChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(SearchKey));

                IsSearching = SongsService.GetIsSearching(SearchKey);

                UpdateSearchSongs();
            }
        }

        public string[] FileMediaSources
        {
            get => fileMediaSources;
            set
            {
                if (value == fileMediaSources) return;

                var args = new ValueChangedEventArgs<string[]>(FileMediaSources, value);
                fileMediaSources = value;
                FileMediaSourcesChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(FileMediaSources));
            }
        }

        public bool IsSearching
        {
            get => isSearching;
            private set
            {
                if (value == isSearching) return;

                isSearching = value;
                OnPropertyChanged(nameof(IsSearching));
            }
        }

        public IEnumerable<Song> ShuffledSongs
        {
            get => shuffledSongs;
            set
            {
                if (ReferenceEquals(value, shuffledSongs)) return;

                shuffledSongs = value;
                OnPropertyChanged(nameof(ShuffledSongs));

                SearchSongs = SongsService.GetSearchSongs(this);
            }
        }

        public IEnumerable<Song> SearchSongs
        {
            get => searchSongs;
            private set
            {
                if (ReferenceEquals(value, searchSongs)) return;

                searchSongs = value.ToBuffer();
                OnPropertyChanged(nameof(SearchSongs));
            }
        }

        public SourcePlaylist(IAudioServiceHelper audioServiceHelper = null, INotifyPropertyChangedHelper notifyHelper = null)
            : base(Guid.Empty, notifyHelper)
        {
            helper = audioServiceHelper;
        }

        private async void UpdateSearchSongs()
        {
            string searchKey = SearchKey;

            Song[] searchSongs = await Task.Run(() => SongsService.GetSearchSongs(this).Take(50).ToArray());
            if (searchKey == SearchKey) SearchSongs = searchSongs;
        }

        protected override void OnSongsChanged()
        {
            base.OnSongsChanged();

            ShuffledSongs = SongsService.GetShuffledSongs(Songs).ToBuffer();
        }

        public virtual void Reload()
        {
            helper?.Reload(this);
        }
    }
}
