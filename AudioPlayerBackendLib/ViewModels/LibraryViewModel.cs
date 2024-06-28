using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using Newtonsoft.Json.Linq;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
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

        public LibraryViewModel(AudioServicesBuildConfig config, IServicedLibraryRepo libraryRepo,
            IServicedPlaylistsRepo playlistsRepo, IPlaylistViewModel playlistViewModel,
            ISongSearchViewModel songSearchViewModel)
        {
            IsLocalFileMediaSource = config.BuildStandalone || config.BuildServer;
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
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

        private void OnPlaylistsChange(object sender, AudioLibraryChangeArgs<IList<PlaylistInfo>> e)
        {
            Playlists = e.NewValue;
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

            IList<PlaylistInfo> playlistInfos = Playlists.Where(x => x.Id != playlistId).ToArray();
            await libraryRepo.SendPlaylistsChange(playlistInfos);
            Playlists = playlistInfos;
        }

        public async Task Dispose()
        {
            libraryRepo.Dispose();
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
