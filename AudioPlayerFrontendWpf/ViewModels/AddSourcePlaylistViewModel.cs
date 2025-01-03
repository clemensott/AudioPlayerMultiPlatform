﻿using AudioPlayerBackend;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
using AudioPlayerBackend.Build;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using StdOttStandard.Linq;

namespace AudioPlayerFrontend.ViewModels
{
    public class AddSourcePlaylistViewModel : INotifyPropertyChanged, IAudioService
    {
        private bool appendSources, newPlaylist;
        private string name;
        private string sources;
        private LoopType loop;
        private OrderType shuffle;
        private PlaylistInfo selectedPlaylistId;
        private ObservableCollection<PlaylistInfo> sourcePlaylists;

        public bool NewPlaylist
        {
            get => newPlaylist;
            set
            {
                if (value == newPlaylist) return;

                newPlaylist = value;
                OnPropertyChanged(nameof(NewPlaylist));
            }
        }

        public bool AppendSources
        {
            get => appendSources;
            set
            {
                if (value == appendSources) return;

                appendSources = value;
                OnPropertyChanged(nameof(AppendSources));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (value == name) return;

                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Sources
        {
            get => sources;
            set
            {
                if (value == sources) return;

                sources = value;
                OnPropertyChanged(nameof(Sources));
            }
        }

        public LoopType Loop
        {
            get => loop;
            set
            {
                if (value == loop) return;

                loop = value;
                OnPropertyChanged(nameof(Loop));
            }
        }

        public OrderType Shuffle
        {
            get => shuffle;
            set
            {
                if (value == shuffle) return;

                shuffle = value;
                OnPropertyChanged(nameof(Shuffle));
            }
        }

        public PlaylistInfo SelectedPlaylistId
        {
            get => selectedPlaylistId;
            set
            {
                if (value == selectedPlaylistId) return;

                selectedPlaylistId = value;
                OnPropertyChanged(nameof(SelectedPlaylistId));
            }
        }

        public ObservableCollection<PlaylistInfo> SourcePlaylists
        {
            get => sourcePlaylists;
            set
            {
                if (value == sourcePlaylists) return;

                sourcePlaylists = value;
                OnPropertyChanged(nameof(SourcePlaylists));
            }
        }

        public ILibraryRepo LibraryRepo { get; }

        public IPlaylistsRepo PlaylistsRepo { get; }

        public AddSourcePlaylistViewModel(AudioServices audioServices)
        {
            LibraryRepo = audioServices.GetLibraryRepo();
            PlaylistsRepo = audioServices.GetPlaylistsRepo();

            Loop = LoopType.CurrentPlaylist;
        }

        public async Task Start()
        {
            PlaylistsRepo.InsertedPlaylist += PlaylistsRepo_InsertedPlaylist;
            PlaylistsRepo.RemovedPlaylist += PlaylistsRepo_RemovedPlaylist;

            Library library = await LibraryRepo.GetLibrary();
            SourcePlaylists = new ObservableCollection<PlaylistInfo>(library.Playlists);
        }

        public Task Stop()
        {
            PlaylistsRepo.InsertedPlaylist -= PlaylistsRepo_InsertedPlaylist;
            PlaylistsRepo.RemovedPlaylist -= PlaylistsRepo_RemovedPlaylist;

            SourcePlaylists.Clear();

            return Task.CompletedTask;
        }

        private void PlaylistsRepo_InsertedPlaylist(object sender, InsertPlaylistArgs e)
        {
            SourcePlaylists.Insert(e.Index ?? SourcePlaylists.Count, e.Playlist.ToPlaylistInfo());
        }

        private void PlaylistsRepo_RemovedPlaylist(object sender, RemovePlaylistArgs e)
        {
            int index = SourcePlaylists.IndexOf(p => p.Id == e.Id);
            SourcePlaylists.RemoveAt(index);
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
