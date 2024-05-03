using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using StdOttStandard.Linq.DataStructures.Observable;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Audio
{
    public class AudioService : IAudioService
    {
        public event EventHandler<ValueChangedEventArgs<bool>> IsSearchShuffleChanged;
        public event EventHandler<ValueChangedEventArgs<string>> SearchKeyChanged;
        public event EventHandler<ValueChangedEventArgs<IPlaylistBase>> CurrentPlaylistChanged;
        public event EventHandler<ValueChangedEventArgs<ISourcePlaylistBase[]>> SourcePlaylistsChanged;
        public event EventHandler<ValueChangedEventArgs<IPlaylistBase[]>> PlaylistsChanged;
        public event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;
        public event EventHandler<ValueChangedEventArgs<byte[]>> AudioDataChanged;

        private readonly IInvokeDispatcherService dispatcher;
        private bool isSearchShuffle, isSearching, isUpdatingSourcePlaylists, isUpdatingPlaylists;
        private string searchKey;
        private PlaybackState playState;
        private FileMediaSourceRoot[] fileMediaSourceRoots;
        private IPlaylist currentPlaylist;
        private ISourcePlaylistBase[] sourcePlaylists;
        private IPlaylistBase[] playlists;
        private byte[] audioData;
        private float volume;
        private IDictionary<ISourcePlaylist, IEnumerable<Song>> shuffledSongs;
        private IEnumerable<Song> allSongs, searchSongs;

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

                var args = new ValueChangedEventArgs<bool>(IsSearchShuffle, value);
                isSearchShuffle = value;
                IsSearchShuffleChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(IsSearchShuffle));

                if (IsSearchShuffle) AllSongs = SongsHelper.GetShuffledSongs(SourcePlaylists).ToBuffer();
                else SearchSongs = SongsHelper.GetSearchSongs(this).ToBuffer();
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

                IsSearching = SongsHelper.GetIsSearching(SearchKey);

                UpdateSearchSongs();
            }
        }

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                if (value == playState) return;

                var args = new ValueChangedEventArgs<PlaybackState>(PlayState, value);
                playState = value;
                PlayStateChanged?.Invoke(this, args);

                OnPlayStateChanged();
                OnPropertyChanged(nameof(PlayState));
            }
        }

        public IPlaylist CurrentPlaylist
        {
            get => currentPlaylist;
            set
            {
                if (value == currentPlaylist) return;

                var args = new ValueChangedEventArgs<IPlaylistBase>(CurrentPlaylist, value);
                currentPlaylist = value;
                CurrentPlaylistChanged?.Invoke(this, args);

                OnCurrentPlaylistChanged();
                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public ObservableCollection<ISourcePlaylist> SourcePlaylists { get; }

        public ObservableCollection<IPlaylist> Playlists { get; }

        public byte[] AudioData
        {
            get => audioData;
            set
            {
                if (value.BothNullOrSequenceEqual(audioData)) return;

                var args = new ValueChangedEventArgs<byte[]>(AudioData, value);
                audioData = value;
                AudioDataChanged?.Invoke(this, args);

                OnAudioDataChanged();
                OnPropertyChanged(nameof(AudioData));
            }
        }

        public float Volume
        {
            get => volume;
            set
            {
                if (value == volume) return;

                var args = new ValueChangedEventArgs<float>(Volume, value);
                volume = value;
                VolumeChanged?.Invoke(this, args);

                OnServiceVolumeChanged();
                OnPropertyChanged(nameof(Volume));
            }
        }

        IPlaylistBase IAudioServiceBase.CurrentPlaylist { get => CurrentPlaylist; set => CurrentPlaylist = (IPlaylist)value; }

        ISourcePlaylistBase[] IAudioServiceBase.SourcePlaylists
        {
            get => sourcePlaylists;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                if (value == sourcePlaylists) return;

                var args = new ValueChangedEventArgs<ISourcePlaylistBase[]>(sourcePlaylists, value);
                sourcePlaylists = value;

                UpdateSourcePlaylists();
                SourcePlaylistsChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(IAudioServiceBase.SourcePlaylists));
            }
        }

        IPlaylistBase[] IAudioServiceBase.Playlists
        {
            get => playlists;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                if (value == playlists) return;

                var args = new ValueChangedEventArgs<IPlaylistBase[]>(playlists, value);
                playlists = value;

                UpdatePlaylists();
                PlaylistsChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(IAudioServiceBase.Playlists));
            }
        }

        public IEnumerable<Song> AllSongs
        {
            get => allSongs;
            private set
            {
                if (ReferenceEquals(value, allSongs)) return;

                allSongs = value.ToBuffer();
                OnPropertyChanged(nameof(AllSongs));

                SearchSongs = SongsHelper.GetSearchSongs(this);
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

        private IAudioServiceBase Base => this;

        public FileMediaSourceRoot[] FileMediaSourceRoots
        {
            get => fileMediaSourceRoots; 
            set
            {
                if(ReferenceEquals(value, fileMediaSourceRoots)) return;

                var args = new ValueChangedEventArgs<FileMediaSourceRoot[]>(fileMediaSourceRoots, value);
                fileMediaSourceRoots = value;
                FileMediaSourceRootsChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(FileMediaSourceRoots));

            }
        }

        public AudioService(IInvokeDispatcherService dispatcher)
        {
            this.dispatcher = dispatcher;
            playState = PlaybackState.Paused;
            shuffledSongs = new Dictionary<ISourcePlaylist, IEnumerable<Song>>();

            SourcePlaylists = new ObservableCollection<ISourcePlaylist>();
            Base.SourcePlaylists = new ISourcePlaylist[0];
            SourcePlaylists.CollectionChanged += SourcePlaylists_CollectionChanged;
            SourcePlaylists.AddedAny += SourcePlaylists_AddedAny;
            SourcePlaylists.RemovedAny += SourcePlaylists_RemovedAny;

            Playlists = new ObservableCollection<IPlaylist>();
            Base.Playlists = new IPlaylist[0];
            Playlists.CollectionChanged += Playlists_CollectionChanged;

            AllSongs = new Song[0];
        }

        private void SourcePlaylists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (isUpdatingSourcePlaylists) return;
            try
            {
                isUpdatingSourcePlaylists = true;

                Base.SourcePlaylists = SourcePlaylists.ToArray();
            }
            finally
            {
                isUpdatingSourcePlaylists = false;
            }
        }

        private void UpdateSourcePlaylists()
        {
            if (isUpdatingSourcePlaylists) return;
            try
            {
                isUpdatingSourcePlaylists = true;

                for (int i = SourcePlaylists.Count - 1; i >= 0; i--)
                {
                    if (!sourcePlaylists.Contains(SourcePlaylists[i])) SourcePlaylists.RemoveAt(i);
                }

                foreach ((int newIndex, ISourcePlaylistBase playlist) in sourcePlaylists.WithIndex())
                {
                    int oldIndex = SourcePlaylists.IndexOf(playlist);
                    if (oldIndex == -1) SourcePlaylists.Insert(newIndex, (ISourcePlaylist)playlist);
                    else if (oldIndex != newIndex) SourcePlaylists.Move(oldIndex, newIndex);
                }
            }
            finally
            {
                isUpdatingSourcePlaylists = false;
            }
        }

        private void Playlists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (isUpdatingPlaylists) return;
            try
            {
                isUpdatingPlaylists = true;

                Base.Playlists = Playlists.ToArray();
            }
            finally
            {
                isUpdatingPlaylists = false;
            }
        }

        private void UpdatePlaylists()
        {
            if (isUpdatingPlaylists) return;
            try
            {
                isUpdatingPlaylists = true;

                for (int i = Playlists.Count - 1; i >= 0; i--)
                {
                    if (!playlists.Contains(Playlists[i])) Playlists.RemoveAt(i);
                }

                foreach ((int newIndex, IPlaylistBase playlist) in playlists.WithIndex())
                {
                    int oldIndex = Playlists.IndexOf(playlist);
                    if (oldIndex == -1) Playlists.Insert(newIndex, (IPlaylist)playlist);
                    else if (oldIndex != newIndex) Playlists.Move(oldIndex, newIndex);
                }
            }
            finally
            {
                isUpdatingPlaylists = false;
            }
        }

        private void SourcePlaylists_AddedAny(object sender, SingleChangeEventArgs<ISourcePlaylist> e)
        {
            e.Item.SongsChanged += SourcePlaylist_SongsChanged;
            shuffledSongs[e.Item] = e.Item.Songs.Shuffle().ToBuffer();
            UpdateAllSongs();
        }

        private void SourcePlaylists_RemovedAny(object sender, SingleChangeEventArgs<ISourcePlaylist> e)
        {
            e.Item.SongsChanged -= SourcePlaylist_SongsChanged;
            shuffledSongs.Remove(e.Item);
            UpdateAllSongs();
        }

        private void SourcePlaylist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            ISourcePlaylist playlist = (ISourcePlaylist)sender;
            shuffledSongs[playlist] = playlist.Songs.Shuffle().ToBuffer();
            UpdateAllSongs();
        }

        private void UpdateAllSongs()
        {
            AllSongs = shuffledSongs.Values.SelectMany(s => s).ToBuffer();
        }

        protected virtual void OnPlayStateChanged() { }

        protected virtual void OnCurrentPlaylistChanged() { }

        protected virtual void OnFormatChanged() { }

        protected virtual void OnAudioDataChanged() { }

        protected virtual void OnServiceVolumeChanged() { }

        public void Continue(Song? currentSong = null)
        {
            IPlaylist currentPlaylist = CurrentPlaylist;
            if (currentPlaylist == null) return;

            if (currentPlaylist.Loop == LoopType.CurrentSong)
            {
                currentPlaylist.WannaSong = RequestSong.Start(currentPlaylist.CurrentSong);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsHelper.GetNextSong(currentPlaylist, currentSong);

            if (currentPlaylist.Loop == LoopType.StopCurrentSong)
            {
                PlayState = PlaybackState.Paused;
                ChangeCurrentSongOrRestart(currentPlaylist, newCurrentSong);
            }
            else if (currentPlaylist.Loop == LoopType.CurrentPlaylist || !overflow)
            {
                ChangeCurrentSongOrRestart(currentPlaylist, newCurrentSong);
            }
            else if (currentPlaylist.Loop == LoopType.Next)
            {
                CurrentPlaylist = this.GetAllPlaylists().Next(currentPlaylist).next;
                ChangeCurrentSongOrRestart(currentPlaylist, newCurrentSong);
            }
            else if (currentPlaylist.Loop == LoopType.Stop)
            {
                CurrentPlaylist = this.GetAllPlaylists().Next(currentPlaylist).next;
                PlayState = PlaybackState.Paused;
                ChangeCurrentSongOrRestart(currentPlaylist, newCurrentSong);
            }
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsHelper.GetNextSong(CurrentPlaylist).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsHelper.GetPreviousSong(CurrentPlaylist).song);
        }

        private static void ChangeCurrentSongOrRestart(IPlaylistBase playlist, Song? newCurrentSong)
        {
            if (playlist != null) playlist.WannaSong = RequestSong.Start(newCurrentSong);
        }

        private async void UpdateSearchSongs()
        {
            string searchKey = SearchKey;

            Song[] searchSongs = await Task.Run(() => SongsHelper.GetSearchSongs(this).Take(50).ToArray());
            if (searchKey == SearchKey) SearchSongs = searchSongs;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ValueChangedEventArgs<FileMediaSourceRoot[]>> FileMediaSourceRootsChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            dispatcher.InvokeDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }
    }
}
