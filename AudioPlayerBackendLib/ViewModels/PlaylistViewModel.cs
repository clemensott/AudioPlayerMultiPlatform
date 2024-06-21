using AudioPlayerBackend.Audio;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class PlaylistViewModel : IPlaylistViewModel
    {
        private readonly IPlaylistsRepo playlistsRepo;
        private bool isRunning, isLoaded;
        private Guid? id;
        private string name;
        private OrderType shuffle;
        private LoopType loop;
        private double playbackRate;
        private TimeSpan position, duration;
        private Song? currentSong;
        private IList<Song> songs;

        public bool IsLoaded
        {
            get => isLoaded;
            set
            {
                if (value == isLoaded) return;

                isLoaded = value;
                OnPropertyChanged(nameof(IsLoaded));
            }
        }

        public Guid? Id
        {
            get => id;
            private set
            {
                if (value == id) return;

                id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => name;
            private set
            {
                if (value == name) return;

                name = value;
                OnPropertyChanged(nameof(Name));
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

        public double PlaybackRate
        {
            get => playbackRate;
            set
            {
                if (value == playbackRate) return;

                playbackRate = value;
                OnPropertyChanged(nameof(PlaybackRate));
            }
        }

        public TimeSpan Position
        {
            get => position;
            set
            {
                if (value == position) return;

                position = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        public TimeSpan Duration
        {
            get => duration;
            set
            {
                if (value == duration) return;

                duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }

        public Song? CurrentSong
        {
            get => currentSong;
            private set
            {
                if (value == currentSong) return;

                currentSong = value;
                OnPropertyChanged(nameof(CurrentSong));
            }
        }

        public IList<Song> Songs
        {
            get => songs;
            private set
            {
                if (value == songs) return;

                songs = value;
                OnPropertyChanged(nameof(Songs));
            }
        }

        public PlaylistViewModel(IPlaylistsRepo playlistsRepo)
        {
            this.playlistsRepo = playlistsRepo;
        }

        public async Task SetPlaylistId(Guid? id)
        {
            Id = Id;
            if (isRunning)
            {
                await LoadPlaylistData();
            }
        }

        public Task SendRequestSong(RequestSong? requestSong)
        {
            throw new NotImplementedException();
        }

        public Task Start()
        {
            isRunning = true;
            
        }

        private async Task LoadPlaylistData()
        {
            if (Id.TryHasValue(out Guid id))
            {
                var playlist = await playlistsRepo.GetPlaylist(id);
                Name = playlist.Name;
                Shuffle = playlist.Shuffle;
                Loop = playlist.Loop;
                PlaybackRate = playlist.PlaybackRate;
                Position = playlist.Position;
                Duration = playlist.Duration;
                CurrentSong = playlist.Songs.Cast<Song?>().FirstOrDefault(s => s?.Id == playlist.CurrentSongId);
                Songs = playlist.Songs;
            }
        }

        public Task Stop()
        {
            isRunning = false;
            
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
