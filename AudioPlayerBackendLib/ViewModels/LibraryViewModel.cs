using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.AudioLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class LibraryViewModel : INotifyPropertyChanged, IAudioService
    {
        private readonly ILibraryRepo libraryRepo;
        private bool isLoaded, isSearching;
        private IList<PlaylistInfo> playlists;
        private IList<SourcePlaylistInfo> sourcePlaylists;
        private IList<FileMediaSourceRoot> fileMediaSourceRoots;

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

        public bool IsSearching
        {
            get => isSearching;
            set
            {
                if (value == isSearching) return;

                isSearching = value;
                OnPropertyChanged(nameof(IsSearching));
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

        public IList<FileMediaSourceRoot> FileMediaSourceRoots
        {
            get => fileMediaSourceRoots;
            set
            {
                if (value == fileMediaSourceRoots) return;

                fileMediaSourceRoots = value;
                OnPropertyChanged(nameof(FileMediaSourceRoots));
            }
        }

        public IList<SourcePlaylistInfo> SourcePlaylists
        {
            get => sourcePlaylists;
            private set
            {
                if (value == sourcePlaylists) return;

                sourcePlaylists = value;
                OnPropertyChanged(nameof(SourcePlaylists));
            }
        }

        public LibraryViewModel(ILibraryRepo libraryRepo, IPlaylistViewModel playlistViewModel)
        {
            this.libraryRepo = libraryRepo;
            CurrentPlaylist = playlistViewModel;
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        public Task Dispose()
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
