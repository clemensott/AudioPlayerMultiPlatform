using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Extensions;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class LibraryViewModel : ILibraryViewModel
    {
        private readonly IServicedLibraryRepo libraryRepo;
        private readonly IServicedPlaylistsRepo playlistsRepo;
        private bool isLoaded;
        private PlaybackState playState;
        private double volume;
        private ObservableCollection<PlaylistInfo> playlists;

        public bool IsLoaded
        {
            get => isLoaded;
            private set
            {
                if (value == isLoaded) return;

                isLoaded = value;
                OnPropertyChanged(nameof(IsLoaded));
            }
        }

        public bool IsLocalFileMediaSource { get; }

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                if (value == playState) return;

                playState = value;
                OnPropertyChanged(nameof(PlayState));
                libraryRepo.SendPlayStateChange(value);
            }
        }

        public double Volume
        {
            get => volume;
            set
            {
                if (value == volume) return;

                volume = value;
                OnPropertyChanged(nameof(Volume));
                libraryRepo.SendVolumeChange(value);
            }
        }

        public int CurrentPlaylistIndex
        {
            get => Playlists.IndexOf(p => p.Id == CurrentPlaylist.Id);
            set
            {
                if (value == CurrentPlaylistIndex) return;

                CurrentPlaylist.SetPlaylistId(Playlists.ElementAtOrDefault(value)?.Id);
                OnPropertyChanged(nameof(CurrentPlaylistIndex));
            }
        }

        public IPlaylistViewModel CurrentPlaylist { get; }

        public ObservableCollection<PlaylistInfo> Playlists
        {
            get => playlists;
            private set
            {
                if (value == playlists) return;

                playlists = value;
                OnPropertyChanged(nameof(Playlists));
            }
        }

        public ISongSearchViewModel SongSearch { get; }

        public LibraryViewModel(AudioServicesBuildConfig config, IServicedLibraryRepo libraryRepo,
            IServicedPlaylistsRepo playlistsRepo, IPlaylistViewModel playlistViewModel,
            ISongSearchViewModel songSearchViewModel)
        {
            IsLocalFileMediaSource = config.BuildStandalone || config.BuildServer;
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            CurrentPlaylist = playlistViewModel;
            SongSearch = songSearchViewModel;
            Playlists = new ObservableCollection<PlaylistInfo>();
        }

        public async Task Start()
        {
            Subscribe();

            Library library = await libraryRepo.GetLibrary();
            PlayState = library.PlayState;
            Volume = library.Volume;
            Playlists = new ObservableCollection<PlaylistInfo>(library.Playlists);
            CurrentPlaylistIndex = Playlists.IndexOf(p => p.Id == library.CurrentPlaylistId);

            await CurrentPlaylist.SetPlaylistId(library.CurrentPlaylistId);
            await CurrentPlaylist.Start();
        }

        public async Task Stop()
        {
            Unsubscribe();

            await CurrentPlaylist.Stop();
        }

        private void Subscribe()
        {
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;

            playlistsRepo.OnInsertPlaylist += PlaylistsRepo_OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist += PlaylistsRepo_OnRemovePlaylist;
        }

        private void Unsubscribe()
        {
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;

            playlistsRepo.OnInsertPlaylist -= PlaylistsRepo_OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist -= PlaylistsRepo_OnRemovePlaylist;
        }

        private void OnPlayStateChange(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            playState = e.NewValue;
            OnPropertyChanged(nameof(PlayState));
        }

        private void OnVolumeChange(object sender, AudioLibraryChangeArgs<double> e)
        {
            volume = e.NewValue;
            OnPropertyChanged(nameof(Volume));
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            await CurrentPlaylist.SetPlaylistId(e.NewValue);
        }

        private void PlaylistsRepo_OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            PlaylistInfo playlistInfo = e.Playlist.ToPlaylistInfo();
            if (e.Index.HasValue) Playlists.Insert(e.Index.Value, playlistInfo);
            else Playlists.Add(playlistInfo);
        }

        private void PlaylistsRepo_OnRemovePlaylist(object sender, RemovePlaylistArgs e)
        {
            int index = Playlists.IndexOf(p => p.Id == e.Id);
            Playlists.RemoveAt(index);
        }

        public async Task RemixSongs(Guid playlistId)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(playlistId);
            await playlistsRepo.SendSongsChange(playlist.Id, playlist.Songs.Shuffle().ToArray());
        }

        public async Task RemovePlaylist(Guid playlistId)
        {
            if (CurrentPlaylist.Id == playlistId)
            {
                int newIndex = Math.Min(CurrentPlaylistIndex + 1, Playlists.Count);
                await CurrentPlaylist.SetPlaylistId(Playlists.ElementAtOrDefault(newIndex)?.Id);
            }

            await playlistsRepo.SendRemovePlaylist(playlistId);

            int index = Playlists.IndexOf(p => p.Id == playlistId);
            Playlists.RemoveAt(index);
        }

        public async Task Dispose()
        {
            await libraryRepo.Dispose();
            await CurrentPlaylist.Dispose();
            await SongSearch.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
