using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using StdOttStandard.Linq;
using StdOttStandard.Linq.Sort;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class SongSearchViewModel : ISongSearchViewModel
    {
        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;
        private readonly IInvokeDispatcherService dispatcher;
        private readonly IDictionary<Guid, ICollection<Song>> allSongs;

        private bool isEnabled, isSearching, isSearchShuffle, isCurrentPlaylist;
        private string searchKey;
        private Guid? currentPlaylistId;
        private IEnumerable<Song> searchSongs;
        private Song[] shuffledSongs;

        public bool IsEnabled
        {
            get => isEnabled;
            private set
            {
                if (value == isEnabled) return;

                isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));

                IsSearching = IsEnabled && SongsHelper.GetIsSearching(SearchKey);
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

        public bool IsSearchShuffle
        {
            get => isSearchShuffle;
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;
                OnPropertyChanged(nameof(IsSearchShuffle));

                UpdateSearchSongs();
            }
        }

        public string SearchKey
        {
            get => searchKey;
            set
            {
                if (value == searchKey) return;

                searchKey = value;
                OnPropertyChanged(nameof(SearchKey));

                IsSearching = IsEnabled && SongsHelper.GetIsSearching(SearchKey);
                UpdateSearchSongs();
            }
        }

        public IEnumerable<Song> SearchSongs
        {
            get => searchSongs;
            private set
            {
                if (value == searchSongs) return;

                searchSongs = value;
                OnPropertyChanged(nameof(SearchSongs));
            }
        }

        public bool IsCurrentPlaylist
        {
            get => isCurrentPlaylist;
            private set
            {
                if (value == isCurrentPlaylist) return;

                bool wasCurrentPlaylist = isCurrentPlaylist;
                isCurrentPlaylist = value;
                OnPropertyChanged(nameof(IsCurrentPlaylist));

                if (wasCurrentPlaylist != isCurrentPlaylist) _ = UpdateSearchSongs();
            }
        }

        public Guid? CurrentPlaylistId
        {
            get => currentPlaylistId;
            private set
            {
                if (value == currentPlaylistId) return;

                currentPlaylistId = value;
                OnPropertyChanged(nameof(CurrentPlaylistId));
                IsCurrentPlaylist = currentPlaylistId.HasValue && currentPlaylistId == SearchPlaylist.Id;
            }
        }

        public IPlaylistViewModel SearchPlaylist { get; }

        public SongSearchViewModel(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo,
            IPlaylistViewModel searchPlaylist, IInvokeDispatcherService dispatcher)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            SearchPlaylist = searchPlaylist;
            this.dispatcher = dispatcher;

            allSongs = new Dictionary<Guid, ICollection<Song>>();
            SearchSongs = Enumerable.Empty<Song>();
        }

        private async Task AddSongsToNewSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType,
            Guid? currentPlaylistId)
        {
            bool setNextSongInOldPlaylist;
            SongRequest? songRequest;
            ICollection<Song> newSongs;

            Playlist currentPlaylist =
                currentPlaylistId.TryHasValue(out Guid id) ? await playlistsRepo.GetPlaylist(id) : null;
            Song? currentSong = currentPlaylist?.GetCurrentSong();

            if (addType == SearchPlaylistAddType.FirstInPlaylist || !currentSong.HasValue)
            {
                setNextSongInOldPlaylist = false;

                newSongs = songs.ToArray();
                songRequest = SongRequest.Start(songs.First().Id);
            }
            else
            {
                setNextSongInOldPlaylist = true;

                songRequest = currentPlaylist.CurrentSongRequest?.CloneWithContinuePlayback();
                newSongs = songs.Insert(0, currentSong.Value).ToArray();
            }

            PlaylistType playlistType = PlaylistType.Custom | PlaylistType.Search;
            Playlist playlist = new Playlist(Guid.NewGuid(), playlistType, "Custom", OrderType.Custom, LoopType.Next, 1,
                songRequest, newSongs, null, null, null, null);

            await playlistsRepo.InsertPlaylist(playlist, null);
            await libraryRepo.SetCurrentPlaylistId(playlist.Id);

            if (setNextSongInOldPlaylist)
            {
                Song? nextSong = currentPlaylist.Songs.Cast<Song?>().NextOrDefault(currentSong).next;
                await playlistsRepo.SetCurrentSongRequest(currentPlaylist.Id, SongRequest.Start(nextSong?.Id));
            }
        }

        private async Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType,
            PlaylistInfo searchPlaylist)
        {
            Song[] newSongs;
            Playlist currentPlaylist = await playlistsRepo.GetPlaylist(searchPlaylist.Id);
            switch (addType)
            {
                case SearchPlaylistAddType.FirstInPlaylist:
                    newSongs = songs.Concat(currentPlaylist.Songs).Distinct().ToArray();
                    await playlistsRepo.SetSongs(currentPlaylist.Id, newSongs);
                    await playlistsRepo.SetCurrentSongRequest(currentPlaylist.Id, SongRequest.Start(songs.First().Id));
                    break;

                case SearchPlaylistAddType.NextInPlaylist:
                    int currentSongIndex =
                        currentPlaylist.Songs.IndexOf(s => s.Id == currentPlaylist.CurrentSongRequest?.Id);
                    IEnumerable<Song> beforeSongs = currentPlaylist.Songs.Take(currentSongIndex + 1);
                    IEnumerable<Song> afterSongs = currentPlaylist.Songs.Skip(currentSongIndex + 1);

                    newSongs = beforeSongs.Concat(songs).Concat(afterSongs).ToArray();
                    await playlistsRepo.SetSongs(currentPlaylist.Id, newSongs);
                    break;

                case SearchPlaylistAddType.LastInPlaylist:
                    newSongs = currentPlaylist.Songs.Concat(songs).Distinct().ToArray();
                    await playlistsRepo.SetSongs(currentPlaylist.Id, newSongs);
                    break;
            }
        }

        private async Task ReplaceSongsInSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType,
            Guid? currentPlaylistId, PlaylistInfo searchPlaylist)
        {
            Playlist currentPlaylist =
                currentPlaylistId.TryHasValue(out Guid id) ? await playlistsRepo.GetPlaylist(id) : null;
            Song? currentSong = currentPlaylist?.GetCurrentSong();

            if (addType == SearchPlaylistAddType.FirstInPlaylist || !currentSong.HasValue)
            {
                await playlistsRepo.SetSongs(searchPlaylist.Id, songs.Distinct().ToArray());
                await playlistsRepo.SetCurrentSongRequest(searchPlaylist.Id, SongRequest.Start(songs.First().Id));

                await libraryRepo.SetCurrentPlaylistId(searchPlaylist.Id);
            }
            else
            {
                Song[] newSongs = songs.Insert(0, currentSong.Value).Distinct().ToArray();
                await playlistsRepo.SetSongs(searchPlaylist.Id, newSongs);

                SongRequest searchRequestSong = currentPlaylist.CurrentSongRequest.Value.CloneWithContinuePlayback();
                await playlistsRepo.SetCurrentSongRequest(searchPlaylist.Id, searchRequestSong);

                await libraryRepo.SetCurrentPlaylistId(searchPlaylist.Id);

                Song? nextSong = SongsHelper.GetNextSong(currentPlaylist, currentSong).song;
                SongRequest? currentPlaylistRequestSong = SongRequest.Start(nextSong?.Id);
                await playlistsRepo.SetCurrentSongRequest(currentPlaylist.Id, currentPlaylistRequestSong);
            }
        }

        public async Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType)
        {
            songs = songs as Song[] ?? songs.ToArray();

            if (!songs.Any()) return;

            Library library = await libraryRepo.GetLibrary();
            PlaylistInfo searchPlaylist = library.Playlists.FirstOrDefault(p => p.Type.HasFlag(PlaylistType.Search));

            if (searchPlaylist == null)
            {
                await AddSongsToNewSearchPlaylist(songs, addType, library.CurrentPlaylistId);
            }
            else if (searchPlaylist.Id == library.CurrentPlaylistId)
            {
                await AddSongsToSearchPlaylist(songs, addType, searchPlaylist);
            }
            else
            {
                await ReplaceSongsInSearchPlaylist(songs, addType, library.CurrentPlaylistId, searchPlaylist);
            }
        }

        private async Task UpdateSearchSongs()
        {
            string searchKey = SearchKey;
            Song[] currentShuffledSongs = shuffledSongs;
            ICollection<Song> searchPlaylistSongs = IsCurrentPlaylist ? SearchPlaylist.Songs : null;

            Song[] searchSongs = await Task.Run(() =>
            {
                IEnumerable<Song> search = SongsHelper.GetFilteredSongs(currentShuffledSongs, searchKey);

                if (searchPlaylistSongs != null)
                {
                    HashSet<Song> excludeSongs = new HashSet<Song>(searchPlaylistSongs);
                    search = search.Where(s => !excludeSongs.Contains(s));
                }

                if (IsSearchShuffle) search = search.HeapSort(currentShuffledSongs.IndexOf);

                return search.Take(50).ToArray();
            });

            if (searchKey == SearchKey) SearchSongs = searchSongs;
        }

        public async Task Start()
        {
            IsEnabled = true;
            SubscribeLibraryRepo();
            SubscribePlaylistsRepo();
            SubscribeSearchPlaylist();

            await SearchPlaylist.Start();
            await LoadAllSongs();
        }

        private void SubscribeLibraryRepo()
        {
            libraryRepo.CurrentPlaylistIdChanged += OnCurrentPlaylistIdChanged;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.CurrentPlaylistIdChanged -= OnCurrentPlaylistIdChanged;
        }

        private void OnCurrentPlaylistIdChanged(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            dispatcher.InvokeDispatcher(() => CurrentPlaylistId = e.NewValue);
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.InsertedPlaylist += OnInsertedPlaylist;
            playlistsRepo.SongsChanged += OnSongsChanged;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.InsertedPlaylist -= OnInsertedPlaylist;
            playlistsRepo.SongsChanged -= OnSongsChanged;
        }

        private void SubscribeSearchPlaylist()
        {
            SearchPlaylist.PropertyChanged += SearchPlaylist_PropertyChanged;
        }

        private void UnsubscribeSearchPlaylist()
        {
            SearchPlaylist.PropertyChanged -= SearchPlaylist_PropertyChanged;
        }

        private async void SearchPlaylist_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchPlaylist.Songs) && isCurrentPlaylist)
            {
                await UpdateSearchSongs();
            }
        }

        private async void OnInsertedPlaylist(object sender, InsertPlaylistArgs e)
        {
            if (e.Playlist.Type.HasFlag(PlaylistType.Search))
            {
                await dispatcher.InvokeDispatcher(async () =>
                {
                    await SearchPlaylist.SetPlaylistId(e.Playlist.Id);
                    IsCurrentPlaylist = CurrentPlaylistId.HasValue && CurrentPlaylistId == SearchPlaylist.Id;
                });
            }
        }

        private async void OnSongsChanged(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            // if playlist is not in all songs than it is not a Source Playlist
            if (!allSongs.ContainsKey(e.Id)) return;

            allSongs[e.Id] = e.NewValue;
            UpdateShuffledSongs();
            await dispatcher.InvokeDispatcher(UpdateSearchSongs);
        }

        private async Task LoadAllSongs()
        {
            Library library = await libraryRepo.GetLibrary();
            if (!IsEnabled) return;

            IEnumerable<Guid> sourcePlaylistIds = library.Playlists
                .Where(p => p.Type.HasFlag(PlaylistType.SourcePlaylist))
                .Select(p => p.Id);
            foreach (Guid id in sourcePlaylistIds)
            {
                Playlist playlist = await playlistsRepo.GetPlaylist(id);
                if (!IsEnabled) return;

                allSongs[playlist.Id] = playlist.Songs;
            }

            UpdateShuffledSongs();

            Guid? searchPlaylistId = library.Playlists.FirstOrDefault(p => p.Type.HasFlag(PlaylistType.Search))?.Id;
            isCurrentPlaylist = searchPlaylistId == library.CurrentPlaylistId;
            await SearchPlaylist.SetPlaylistId(searchPlaylistId);

            await dispatcher.InvokeDispatcher(UpdateSearchSongs);
        }

        private void UpdateShuffledSongs()
        {
            shuffledSongs = SongsHelper.GetShuffledSongs(allSongs.Values).Select((s, i) => s.WithIndex(i)).ToArray();
        }

        public Task Stop()
        {
            IsEnabled = false;

            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();
            UnsubscribeSearchPlaylist();

            allSongs.Clear();
            shuffledSongs = Array.Empty<Song>();
            SearchSongs = Enumerable.Empty<Song>();
            SearchPlaylist.Stop();

            return Task.CompletedTask;
        }

        public async Task Dispose()
        {
            await Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}