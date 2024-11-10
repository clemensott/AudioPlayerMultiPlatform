using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class LibraryViewModel : ILibraryViewModel
    {
        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;
        private readonly IUpdateLibraryService updateLibraryService;
        private bool isLoaded, isUpdatingPlaylist, isUpdatingPlaylists;
        private int currentPlaylistIndex, playlistsUpdatingCount;
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

        public bool IsClient { get; }

        public bool IsLocalFileMediaSource { get; }

        public bool IsUpdatingPlaylists
        {
            get => isUpdatingPlaylists;
            private set
            {
                if (value == isUpdatingPlaylists) return;

                isUpdatingPlaylists = value;
                OnPropertyChanged(nameof(IsUpdatingPlaylists));
            }
        }

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                if (value == playState) return;

                playState = value;
                OnPropertyChanged(nameof(PlayState));

                if (IsLoaded) libraryRepo.SendPlayStateChange(value);
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

                if (IsLoaded) libraryRepo.SendVolumeChange(value);
            }
        }

        public int CurrentPlaylistIndex
        {
            get => currentPlaylistIndex;
            set
            {
                if (isUpdatingPlaylist || value == CurrentPlaylistIndex) return;

                currentPlaylistIndex = value;
                OnPropertyChanged(nameof(CurrentPlaylistIndex));

                if (IsLoaded) libraryRepo.SendCurrentPlaylistIdChange(Playlists.ElementAtOrDefault(value)?.Id);
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

        public LibraryViewModel(AudioServicesBuildConfig config, ILibraryRepo libraryRepo,
            IPlaylistsRepo playlistsRepo, IPlaylistViewModel playlistViewModel,
            ISongSearchViewModel songSearchViewModel, IUpdateLibraryService updateLibraryService)
        {
            IsClient = config.BuildClient;
            IsLocalFileMediaSource = config.BuildStandalone || config.BuildServer;

            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            this.updateLibraryService = updateLibraryService;

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

            await CurrentPlaylist.SetPlaylistId(library.CurrentPlaylistId);
            UpdateCurrentPlaylistIndex();
            await CurrentPlaylist.Start();

            IsLoaded = true;
        }

        public async Task Stop()
        {
            Unsubscribe();

            IsLoaded = false;

            Playlists = new ObservableCollection<PlaylistInfo>();
            await CurrentPlaylist.Stop();
        }

        private void Subscribe()
        {
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;

            playlistsRepo.OnInsertPlaylist += PlaylistsRepo_OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist += PlaylistsRepo_OnRemovePlaylist;
            playlistsRepo.OnSongsChange += PlaylistsRepo_OnSongsChange;

            updateLibraryService.UpdateStarted += UpdateLibraryService_UpdateStarted;
            updateLibraryService.UpdateCompleted += UpdateLibraryService_UpdateCompleted;
        }

        private void UpdateLibraryService_UpdateStarted(object sender, EventArgs e)
        {
            IsUpdatingPlaylists = ++playlistsUpdatingCount > 0;
        }

        private async void UpdateLibraryService_UpdateCompleted(object sender, EventArgs e)
        {
            // make sure that IsUpdatingPlaylists is at least 1 second set to true
            await Task.Delay(1000);

            IsUpdatingPlaylists = --playlistsUpdatingCount > 0;
        }

        private void Unsubscribe()
        {
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;

            playlistsRepo.OnInsertPlaylist -= PlaylistsRepo_OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist -= PlaylistsRepo_OnRemovePlaylist;
            playlistsRepo.OnSongsChange -= PlaylistsRepo_OnSongsChange;
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
            UpdateCurrentPlaylistIndex();
        }

        private void PlaylistsRepo_OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            Playlists.Insert(e.Index ?? Playlists.Count, e.Playlist.ToPlaylistInfo());
        }

        private void PlaylistsRepo_OnRemovePlaylist(object sender, RemovePlaylistArgs e)
        {
            int index = Playlists.IndexOf(p => p.Id == e.Id);
            Playlists.RemoveAt(index);
        }

        private void PlaylistsRepo_OnSongsChange(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            try
            {
                isUpdatingPlaylist = true;

                int index = Playlists.IndexOf(p => p.Id == e.Id);
                bool restoreCurrentPlaylistIndex = index == CurrentPlaylistIndex;
                PlaylistInfo playlist = Playlists[index];
                Playlists[index] = new PlaylistInfo(playlist.Id, playlist.Type, playlist.Name, e.NewValue.Count,
                    playlist.FilesLastUpdated, playlist.SongsLastUpdated);

                // updating the selected playlist of an listbox triggers a selection change. This restores the selection.
                if (restoreCurrentPlaylistIndex)
                {
                    try
                    {
                        currentPlaylistIndex = -2;
                        OnPropertyChanged(nameof(CurrentPlaylistIndex));
                    }
                    // This workaround is only needed für WPF and throws an error for UWP
                    catch { }

                    UpdateCurrentPlaylistIndex();
                }
            }
            finally
            {
                isUpdatingPlaylist = false;
            }
        }

        private void UpdateCurrentPlaylistIndex()
        {
            currentPlaylistIndex = Playlists.IndexOf(p => p.Id == CurrentPlaylist.Id);
            OnPropertyChanged(nameof(CurrentPlaylistIndex));
        }

        public async Task RemixSongs(Guid playlistId)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(playlistId);
            Song[] remixedSongs = playlist.Songs.Shuffle().Select((song, i) =>
            {
                song.Index = i;
                return song;
            }).ToArray();
            await playlistsRepo.SendSongsChange(playlist.Id, remixedSongs);
        }

        public async Task RemovePlaylist(Guid playlistId)
        {
            if (CurrentPlaylist.Id == playlistId)
            {
                int newIndex = Math.Min(CurrentPlaylistIndex + 1, Playlists.Count - 2);
                await libraryRepo.SendCurrentPlaylistIdChange(Playlists.ElementAtOrDefault(newIndex)?.Id);
            }

            await playlistsRepo.SendRemovePlaylist(playlistId);
        }

        public async Task Dispose()
        {
            await Stop();

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
