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
        public event EventHandler<ValueChangedEventArgs<WaveFormat>> AudioFormatChanged;
        public event EventHandler<ValueChangedEventArgs<byte[]>> AudioDataChanged;

        private readonly INotifyPropertyChangedHelper helper;
        private bool isSearchShuffle, isSearching, isUpdatingSourcePlaylists, isUpdatingPlaylists;
        private string searchKey;
        private PlaybackState playState;
        private IPlaylist currentPlaylist;
        private ISourcePlaylistBase[] sourcePlaylists;
        private IPlaylistBase[] playlists;
        private WaveFormat audioFormat;
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

                if (IsSearchShuffle) AllSongs = SongsService.GetShuffledSongs(SourcePlaylists).ToBuffer();
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

        public WaveFormat AudioFormat
        {
            get => audioFormat;
            set
            {
                if (value == audioFormat) return;

                var args = new ValueChangedEventArgs<WaveFormat>(AudioFormat, value);
                audioFormat = value;
                AudioFormatChanged?.Invoke(this, args);

                OnFormatChanged();
                OnPropertyChanged(nameof(AudioFormat));
            }
        }

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

        private IAudioServiceBase Base => this;

        public AudioService(INotifyPropertyChangedHelper notifyHelper = null)
        {
            helper = notifyHelper;
            playState = PlaybackState.Stopped;
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

                for (int i = 0; i < sourcePlaylists.Length && i < SourcePlaylists.Count; i++)
                {
                    SourcePlaylists[i] = (ISourcePlaylist)sourcePlaylists[i];
                }

                SourcePlaylists.RemoveLastToCount<ISourcePlaylist>(sourcePlaylists.Length);

                while (SourcePlaylists.Count < sourcePlaylists.Length)
                {
                    SourcePlaylists.Add((ISourcePlaylist)sourcePlaylists[SourcePlaylists.Count]);
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

                for (int i = 0; i < playlists.Length && i < Playlists.Count; i++)
                {
                    Playlists[i] = (IPlaylist)playlists[i];
                }

                Playlists.RemoveLastToCount<IPlaylist>(playlists.Length);

                while (Playlists.Count < playlists.Length)
                {
                    Playlists.Add((IPlaylist)playlists[Playlists.Count]);
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

        public void Continue()
        {
            IPlaylist currentPlaylist = CurrentPlaylist;
            if (currentPlaylist == null) return;

            if (currentPlaylist.Loop == LoopType.CurrentSong)
            {
                currentPlaylist.WannaSong = RequestSong.Start(currentPlaylist.CurrentSong);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsService.GetNextSong(currentPlaylist);

            if (currentPlaylist.Loop == LoopType.StopCurrentSong)
            {
                PlayState = PlaybackState.Stopped;
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
                PlayState = PlaybackState.Stopped;
                ChangeCurrentSongOrRestart(currentPlaylist, newCurrentSong);
            }
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetNextSong(CurrentPlaylist).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(CurrentPlaylist, SongsService.GetPreviousSong(CurrentPlaylist).song);
        }

        private static void ChangeCurrentSongOrRestart(IPlaylistBase playlist, Song? newCurrentSong)
        {
            playlist.WannaSong = RequestSong.Start(newCurrentSong);
        }

        private async void UpdateSearchSongs()
        {
            string searchKey = SearchKey;

            Song[] searchSongs = await Task.Run(() => SongsService.GetSearchSongs(this).Take(50).ToArray());
            if (searchKey == SearchKey) SearchSongs = searchSongs;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            if (helper?.InvokeDispatcher != null) helper.InvokeDispatcher(Raise);
            else Raise();

            void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
