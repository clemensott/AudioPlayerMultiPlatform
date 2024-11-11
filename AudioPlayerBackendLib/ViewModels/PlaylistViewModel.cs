using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class PlaylistViewModel : IPlaylistViewModel
    {
        private readonly IPlaylistsRepo playlistsRepo;
        private bool isRunning, isLoaded;
        private Guid? id;
        private PlaylistType type;
        private string name;
        private OrderType shuffle;
        private LoopType loop;
        private double playbackRate;
        private Song? currentSong;
        private SongRequest? currentSongRequest;
        private ICollection<Song> shuffledSongs, songs;

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

        public PlaylistType Type
        {
            get => type;
            private set
            {
                if (value == type) return;

                type = value;
                OnPropertyChanged(nameof(Type));
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

                if (isRunning)
                {
                    SendShuffle(value);
                    UpdateSongs();
                }
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

                if (isRunning) SendLoop(value);
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

                if (isRunning) SendPlaybackRate(value);
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

        public SongRequest? CurrentSongRequest
        {
            get => currentSongRequest;
            set
            {
                if (Equals(value, currentSongRequest)) return;

                currentSongRequest = value;
                OnPropertyChanged(nameof(CurrentSongRequest));

                CurrentSong = shuffledSongs.Cast<Song?>().FirstOrDefault(s => s?.Id == currentSongRequest?.Id);

                if (isRunning) SetCurrentSongRequest(value);
            }
        }

        public ICollection<Song> Songs
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

        public async Task SendShuffle(OrderType shuffle)
        {
            if (Id.TryHasValue(out Guid id)) await playlistsRepo.SetShuffle(id, shuffle);
        }

        public async Task SendLoop(LoopType loop)
        {
            if (Id.TryHasValue(out Guid id)) await playlistsRepo.SetLoop(id, loop);
        }

        public async Task SendPlaybackRate(double playbackRate)
        {
            if (Id.TryHasValue(out Guid id)) await playlistsRepo.SetPlaybackRate(id, playbackRate);
        }

        public async Task SetCurrentSongRequest(SongRequest? songRequest)
        {
            if (Id.TryHasValue(out Guid id)) await playlistsRepo.SetCurrentSongRequest(id, songRequest);
        }

        public async Task SetPlaylistId(Guid? id)
        {
            if (Id == id) return;

            Id = id;
            if (isRunning)
            {
                await LoadPlaylistData();
            }
        }

        public async Task Start()
        {
            isRunning = true;

            playlistsRepo.NameChanged += OnNameChanged;
            playlistsRepo.ShuffleChanged += OnShuffleChanged;
            playlistsRepo.LoopChanged += OnLoopChanged;
            playlistsRepo.PlaybackRateChanged += OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged += OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged += OnSongsChanged;

            await LoadPlaylistData();
        }

        public Task Stop()
        {
            isRunning = false;

            playlistsRepo.NameChanged -= OnNameChanged;
            playlistsRepo.ShuffleChanged -= OnShuffleChanged;
            playlistsRepo.LoopChanged -= OnLoopChanged;
            playlistsRepo.PlaybackRateChanged -= OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged -= OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged -= OnSongsChanged;

            Name = string.Empty;
            Shuffle = OrderType.ByTitleAndArtist;
            Loop = LoopType.CurrentPlaylist;
            PlaybackRate = 1;
            CurrentSong = null;
            shuffledSongs = Array.Empty<Song>();
            Songs = Array.Empty<Song>();

            IsLoaded = false;

            return Task.CompletedTask;
        }

        private void OnNameChanged(object sender, PlaylistChangeArgs<string> e)
        {
            if (Id == e.Id) Name = e.NewValue;
        }

        private void OnShuffleChanged(object sender, PlaylistChangeArgs<OrderType> e)
        {
            if (Id == e.Id) Shuffle = e.NewValue;
        }

        private void OnLoopChanged(object sender, PlaylistChangeArgs<LoopType> e)
        {
            if (Id == e.Id) Loop = e.NewValue;
        }

        private void OnPlaybackRateChanged(object sender, PlaylistChangeArgs<double> e)
        {
            if (Id == e.Id) PlaybackRate = e.NewValue;
        }

        private void OnCurrentSongRequestChanged(object sender, PlaylistChangeArgs<SongRequest?> e)
        {
            if (Id == e.Id) CurrentSongRequest = e.NewValue;
        }

        private void OnSongsChanged(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            if (Id == e.Id)
            {
                shuffledSongs = e.NewValue;
                UpdateSongs();
            }
        }

        private async Task LoadPlaylistData()
        {
            if (Id.TryHasValue(out Guid id))
            {
                Playlist playlist = await playlistsRepo.GetPlaylist(id);
                Name = playlist.Name;
                Type = playlist.Type;
                Shuffle = playlist.Shuffle;
                Loop = playlist.Loop;
                PlaybackRate = playlist.PlaybackRate;
                shuffledSongs = playlist.Songs;
                CurrentSongRequest = playlist.CurrentSongRequest;
                UpdateSongs();

                IsLoaded = true;
            }
        }

        private void UpdateSongs()
        {
            Songs = SongsHelper.GetAllSongs(shuffledSongs.ToNotNull(), Shuffle).ToArray();
        }

        public async Task RemoveSong(Guid songId)
        {
            if (!Id.TryHasValue(out Guid id)) return;

            Song[] newSongs = Songs.Where(s => s.Id != songId).ToArray();
            if (Songs.SequenceEqual(newSongs)) return;

            await playlistsRepo.SetSongs(id, newSongs);
        }

        public async Task ClearSongs()
        {
            if (Id.TryHasValue(out Guid id)) await playlistsRepo.SetSongs(id, new Song[0]);
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
