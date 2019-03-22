using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
        public static SourcePlaylist GetInstance(IAudioServiceHelper helper = null)
        {
            Playlist playlist;

            if (Playlist.TryGetInstance(Guid.Empty, out playlist)) return (SourcePlaylist)playlist;

            return new SourcePlaylist(helper);
        }

        private readonly IAudioServiceHelper helper;
        private bool isSearchShuffle, isSearching;
        private string searchKey;
        private string[] fileMediaSources;
        private Song[] shuffledSongs;
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

                if (IsSearchShuffle) ShuffledSongs = SongsService.GetShuffledSongs(Songs).ToArray();
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
                SearchSongs = SongsService.GetSearchSongs(this);
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

        public Song[] ShuffledSongs
        {
            get => shuffledSongs;
            set
            {
                if (value == shuffledSongs) return;

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

                searchSongs = value;
                OnPropertyChanged(nameof(SearchSongs));
            }
        }

        private SourcePlaylist(IAudioServiceHelper helper = null) : base(Guid.Empty)
        {
            this.helper = helper;
        }

        protected override void OnSongsChanged()
        {
            base.OnSongsChanged();

            ShuffledSongs = SongsService.GetShuffledSongs(Songs).ToArray();
        }

        public virtual void Reload()
        {
            helper?.Reload(this);
        }
    }
}
