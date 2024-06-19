using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class LibraryViewModel : ILibraryViewModel
    {
        private readonly ILibraryRepo libraryRepo;
        private bool isLoaded;
        private PlaybackState playState;
        private double volume;
        private IList<PlaylistInfo> playlists;

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

        public IPlaylistViewModel CurrentPlaylist { get; }

        public IList<PlaylistInfo> Playlists
        {
            get => playlists;
            private set
            {
                if (value == playlists) return;

                playlists = value;
                OnPropertyChanged(nameof(Playlists));
            }
        }

        public ISongSearchViewModel SongSearuch { get; }

        public LibraryViewModel(AudioServicesBuildConfig config, ILibraryRepo libraryRepo,
            IPlaylistViewModel playlistViewModel, ISongSearchViewModel songSearchViewModel)
        {
            IsLocalFileMediaSource = config.BuildStandalone || config.BuildServer;
            this.libraryRepo = libraryRepo;
            CurrentPlaylist = playlistViewModel;
            SongSearuch = songSearchViewModel;
        }

        public async Task Start()
        {
            SubscribeLibraryRepo();

            Library library = await libraryRepo.GetLibrary();
            PlayState = library.PlayState;
            Volume = library.Volume;
            Playlists = library.Playlists;

            await CurrentPlaylist.SetPlaylistId(library.CurrentPlaylistId);
            await CurrentPlaylist.Start();
        }

        public async Task Stop()
        {
            UnsubscribeLibraryRepo();

            await CurrentPlaylist.Stop();
        }

        private void SubscribeLibraryRepo()
        {
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;
            libraryRepo.OnPlaylistsChange += OnPlaylistsChange;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
            libraryRepo.OnPlaylistsChange -= OnPlaylistsChange;
        }

        private void OnPlayStateChange(object sender, AudioLibraryChange<PlaybackState> e)
        {
            playState = e.NewValue;
            OnPropertyChanged(nameof(PlayState));
        }

        private void OnVolumeChange(object sender, AudioLibraryChange<double> e)
        {
            volume = e.NewValue;
            OnPropertyChanged(nameof(Volume));
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChange<Guid?> e)
        {
            await CurrentPlaylist.SetPlaylistId(e.NewValue);
        }

        private void OnPlaylistsChange(object sender, AudioLibraryChange<IList<PlaylistInfo>> e)
        {
            Playlists = e.NewValue;
        }

        public async Task Dispose()
        {
            await CurrentPlaylist.Dispose();
            SongSearuch.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
