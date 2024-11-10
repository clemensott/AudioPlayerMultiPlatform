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
        private readonly IDictionary<Guid, ICollection<Song>> allSongs;

        private bool isEnabled, isSearching, isSearchShuffle, isCurrentPlaylist;
        private string searchKey;
        private IEnumerable<Song> searchSongs, shuffledSongs;

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

        public IPlaylistViewModel SearchPlaylist { get; }

        public SongSearchViewModel(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo, IPlaylistViewModel searchPlaylist)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            SearchPlaylist = searchPlaylist;

            allSongs = new Dictionary<Guid, ICollection<Song>>();
            SearchSongs = Enumerable.Empty<Song>();
        }

        private async Task AddSongsToNewSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType, Guid? currentPlaylistId)
        {
            bool setNextSongInOldPlaylist;
            TimeSpan position, duration;
            RequestSong? requestedSong;
            ICollection<Song> newSongs;

            Playlist currentPlaylist = currentPlaylistId.TryHasValue(out Guid id) ? await playlistsRepo.GetPlaylist(id) : null;
            Song? currentSong = currentPlaylist?.GetCurrentSong();

            if (addType == SearchPlaylistAddType.FirstInPlaylist || !currentSong.HasValue)
            {
                setNextSongInOldPlaylist = false;

                newSongs = songs.ToArray();
                requestedSong = RequestSong.Start(songs.First());
            }
            else
            {
                setNextSongInOldPlaylist = true;

                requestedSong = RequestSong.Get(currentSong.Value, currentPlaylist.Position, currentPlaylist.Duration, true);
                newSongs = songs.Insert(0, currentSong.Value).ToArray();
                position = currentPlaylist.Position;
                duration = currentPlaylist.Duration;
            }

            PlaylistType playlistType = PlaylistType.Custom | PlaylistType.Search;
            Playlist playlist = new Playlist(Guid.NewGuid(), playlistType, "Custom", OrderType.Custom, LoopType.Next, 1,
                 position, duration, requestedSong, null, newSongs, null, null, null, null);

            await playlistsRepo.SendInsertPlaylist(playlist, null);
            await libraryRepo.SendCurrentPlaylistIdChange(playlist.Id);

            if (setNextSongInOldPlaylist)
            {
                Song? nextSong = currentPlaylist.Songs.Cast<Song?>().NextOrDefault(currentSong).next;
                await playlistsRepo.SendCurrentSongIdChange(currentPlaylist.Id, nextSong?.Id);

                await playlistsRepo.SendPositionChange(currentPlaylist.Id, TimeSpan.Zero);
                await playlistsRepo.SendRequestSongChange(currentPlaylist.Id, RequestSong.Start(nextSong));
            }
        }

        private async Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType, PlaylistInfo searchPlaylist)
        {
            Song[] newSongs;
            Playlist currentPlaylist = await playlistsRepo.GetPlaylist(searchPlaylist.Id);
            switch (addType)
            {
                case SearchPlaylistAddType.FirstInPlaylist:
                    newSongs = songs.Concat(currentPlaylist.Songs).Distinct().ToArray();
                    await playlistsRepo.SendSongsChange(currentPlaylist.Id, newSongs);
                    await playlistsRepo.SendRequestSongChange(currentPlaylist.Id, RequestSong.Start(songs.First()));
                    break;

                case SearchPlaylistAddType.NextInPlaylist:
                    int currentSongIndex = currentPlaylist.Songs.IndexOf(s => s.Id == currentPlaylist.CurrentSongId);
                    IEnumerable<Song> beforeSongs = currentPlaylist.Songs.Take(currentSongIndex + 1);
                    IEnumerable<Song> afterSongs = currentPlaylist.Songs.Skip(currentSongIndex + 1);

                    newSongs = beforeSongs.Concat(songs).Concat(afterSongs).ToArray();
                    await playlistsRepo.SendSongsChange(currentPlaylist.Id, newSongs);
                    break;

                case SearchPlaylistAddType.LastInPlaylist:
                    newSongs = currentPlaylist.Songs.Concat(songs).Distinct().ToArray();
                    await playlistsRepo.SendSongsChange(currentPlaylist.Id, newSongs);
                    break;
            }
        }

        private async Task ReplaceSongsInSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType,
            Guid? currentPlaylistId, PlaylistInfo searchPlaylist)
        {
            Playlist currentPlaylist = currentPlaylistId.TryHasValue(out Guid id) ? await playlistsRepo.GetPlaylist(id) : null;
            Song? currentSong = currentPlaylist?.GetCurrentSong();
            if (addType == SearchPlaylistAddType.FirstInPlaylist || !currentSong.HasValue)
            {
                await playlistsRepo.SendSongsChange(searchPlaylist.Id, songs.Distinct().ToArray());
                await playlistsRepo.SendRequestSongChange(searchPlaylist.Id, RequestSong.Start(songs.First()));
                await playlistsRepo.SendDurationChange(searchPlaylist.Id, currentPlaylist.Duration);
                await playlistsRepo.SendPositionChange(searchPlaylist.Id, currentPlaylist.Position);

                await libraryRepo.SendCurrentPlaylistIdChange(searchPlaylist.Id);
            }
            else
            {
                Song[] newSongs = songs.Insert(0, currentSong.Value).Distinct().ToArray();
                await playlistsRepo.SendSongsChange(searchPlaylist.Id, newSongs);

                RequestSong searchRequestSong = RequestSong.Get(currentSong.Value, currentPlaylist.Position, currentPlaylist.Duration, true);
                await playlistsRepo.SendRequestSongChange(searchPlaylist.Id, searchRequestSong);

                await libraryRepo.SendCurrentPlaylistIdChange(searchPlaylist.Id);

                RequestSong? currentPlaylistRequestSong = RequestSong.Start(SongsHelper.GetNextSong(currentPlaylist).song);
                await playlistsRepo.SendRequestSongChange(currentPlaylist.Id, currentPlaylistRequestSong);
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
            IEnumerable<Song> currentShuffledSongs = shuffledSongs;
            ICollection<Song> searchPlaylistSongs = isCurrentPlaylist ? SearchPlaylist.Songs : null;

            Song[] searchSongs = await Task.Run(() =>
            {
                IEnumerable<Song> search = SongsHelper.GetFilteredSongs(currentShuffledSongs, searchKey);

                if (searchPlaylistSongs != null)
                {
                    HashSet<Song> excludeSongs = new HashSet<Song>(searchPlaylistSongs);
                    search = search.Where(s => !excludeSongs.Contains(s));
                }

                search = search.Take(50);

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
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            bool wasCurrentPlaylist = isCurrentPlaylist;
            isCurrentPlaylist = e.NewValue.HasValue && e.NewValue == SearchPlaylist.Id;

            if (wasCurrentPlaylist != isCurrentPlaylist) await UpdateSearchSongs();
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist += OnInsertPlaylist;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist -= OnInsertPlaylist;
            playlistsRepo.OnSongsChange -= OnSongsChange;
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

        private async void OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            if (e.Playlist.Type.HasFlag(PlaylistType.Search)) await SearchPlaylist.SetPlaylistId(e.Playlist.Id);
        }

        private async void OnSongsChange(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            // if playlist is not in all songs than it is not a Source Playlist
            if (!allSongs.ContainsKey(e.Id)) return;

            allSongs[e.Id] = e.NewValue;
            UpdateShuffledSongs();
            await UpdateSearchSongs();
        }

        private async Task LoadAllSongs()
        {
            Library library = await libraryRepo.GetLibrary();
            if (!IsEnabled) return;

            foreach (Guid id in library.Playlists.Where(p => p.Type.HasFlag(PlaylistType.SourcePlaylist)).Select(p => p.Id))
            {
                Playlist playlist = await playlistsRepo.GetPlaylist(id);
                if (!IsEnabled) return;

                allSongs[playlist.Id] = playlist.Songs;
            }

            UpdateShuffledSongs();

            Guid? searchPlaylistId = library.Playlists.FirstOrDefault(p => p.Type.HasFlag(PlaylistType.Search))?.Id;
            isCurrentPlaylist = searchPlaylistId == library.CurrentPlaylistId;
            await SearchPlaylist.SetPlaylistId(searchPlaylistId);

            await UpdateSearchSongs();
        }

        private void UpdateShuffledSongs()
        {
            shuffledSongs = SongsHelper.GetShuffledSongs(allSongs.Values).ToArray();
        }

        public Task Stop()
        {
            IsEnabled = false;

            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();
            UnsubscribeSearchPlaylist();

            allSongs.Clear();
            shuffledSongs = Enumerable.Empty<Song>();
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
