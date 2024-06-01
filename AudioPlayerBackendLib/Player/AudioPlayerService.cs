using AudioPlayerBackend.Audio;
using AudioPlayerBackend.AudioLibrary;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Player
{
    public class AudioPlayerService : IPlayerService
    {
        private const int updateInterval = 100;

        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;
        private readonly IInvokeDispatcherService dispatcher;
        private bool isSetCurrentSong;
        private int errorCount;
        private readonly Timer timer;

        private Guid? currentPlaylistId;
        private IList<Guid> playlistIds;
        private OrderType shuffle;
        private LoopType loop;
        private RequestSong? requestSong;
        private Guid? currentSongId;
        private IList<Song> songs;

        public IPlayer Player { get; }

        public AudioPlayerService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo, IPlayer player, IInvokeDispatcherService dispatcher)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            Player = player;
            this.dispatcher = dispatcher;

            timer = new Timer(Timer_Elapsed, null, -1, updateInterval);
        }

        public async Task Start()
        {
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaFailed += Player_MediaFailed;
            Player.MediaEnded += Player_MediaEnded;

            Subscribe(libraryRepo);
            Subscribe(playlistsRepo);

            Library library = await libraryRepo.GetLibrary();
            currentPlaylistId = library.CurrentPlaylistId;
            playlistIds = library.Playlists;

            await LoadPlaylistData(currentPlaylistId);

            Player.PlayState = library.PlayState;
            await libraryRepo.SendVolumeChange(Player.Volume);


        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        private async Task LoadPlaylistData(Guid? playlistId)
        {
            currentPlaylistId = playlistId;
            if (playlistId.HasValue)
            {
                var currentPlaylist = await playlistsRepo.GetPlaylist(playlistId.Value);
                shuffle = currentPlaylist.Shuffle;
                loop = currentPlaylist.Loop;
                songs = currentPlaylist.Songs;
            }
            else songs = new Song[0];

            CheckUpdateCurrentSong();
        }

        private async void CheckUpdateCurrentSong()
        {
            if (Player.Source.HasValue ^ requestSong.HasValue)
            {
                await UpdateCurrentSong();
            }
        }

        private async void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            errorCount = 0;

            EnableTimer();

            if (currentPlaylistId.HasValue && requestSong?.Song == e.Source)
            {
                Guid playlistId = currentPlaylistId.Value;

                await playlistsRepo.SendCurrentSongIdChange(playlistId, e.Source.Id);
                await playlistsRepo.SendPositionChange(playlistId, e.Position);
                await playlistsRepo.SendDurationChange(playlistId, e.Duration);
            }
        }

        private void Player_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            StopTimer();
            if (++errorCount < 10) Continue(e.Song);
        }

        private void Player_MediaEnded(object sender, MediaEndedEventArgs e)
        {
            Logs.Log($"Player_MediaEnded: {e.Song?.FullPath}");
            StopTimer();
            Service.Continue(e.Song);
        }

        private void Timer_Elapsed(object state)
        {
            dispatcher.InvokeDispatcher(UpdatePosition);
        }

        private void UpdatePosition()
        {
            try
            {
                if (!Player.Source.HasValue ||
                    Player.Source != Service.CurrentPlaylist?.CurrentSong) return;

                TimeSpan position = Service.CurrentPlaylist.Position;
                if (Service.CurrentPlaylist.Position.Seconds == Player.Position.Seconds) return;

                Service.CurrentPlaylist.Position = Player.Position;
                Service.CurrentPlaylist.Duration = Player.Duration;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private void Subscribe(ILibraryRepo libraryRepo)
        {
            if (libraryRepo == null) return;

            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
            libraryRepo.OnPlaylistsChange += OnPlaylistsChange;
        }

        private void Unsubscribe(ILibraryRepo libraryRepo)
        {
            if (libraryRepo == null) return;

            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnPlaylistsChange -= OnPlaylistsChange;
        }

        private void Subscribe(IPlaylistsRepo playlistsRepo)
        {
            if (playlistsRepo == null) return;

            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void Unsubscribe(IPlaylistsRepo playlistsRepo)
        {
            if (playlistsRepo == null) return;

            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void OnCurrentPlaylistIdChange(object sender, AudioLibraryChange<Guid?> e)
        {
            throw new NotImplementedException();
        }

        private void OnPlayStateChange(object sender, AudioLibraryChange<PlaybackState> e)
        {
            throw new NotImplementedException();
        }

        private void OnVolumeChange(object sender, AudioLibraryChange<double> e)
        {
            throw new NotImplementedException();
        }

        private void OnPlaylistsChange(object sender, AudioLibraryChange<IList<PlaylistInfo>> e)
        {
            throw new NotImplementedException();
        }

        private void OnPositionChange(object sender, PlaylistChange<TimeSpan> e)
        {
            throw new NotImplementedException();
        }

        private void OnPlaybackRateChange(object sender, PlaylistChange<double> e)
        {
            throw new NotImplementedException();
        }

        private void OnRequestSongChange(object sender, PlaylistChange<RequestSong?> e)
        {
            throw new NotImplementedException();
        }

        private void OnSongsChange(object sender, PlaylistChange<System.Collections.Generic.IList<Song>> e)
        {
            throw new NotImplementedException();
        }

        private async void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            if (e.OldValue != null)
            {
                e.OldValue.WannaSong = RequestSong.Get(e.OldValue.CurrentSong, e.OldValue.Position, e.OldValue.Duration);
            }

            CheckCurrentSong(Service.CurrentPlaylist);
            await UpdateCurrentSong();
        }

        private void Service_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            Player.PlayState = e.NewValue;

            EnableTimer();
        }

        private void Service_VolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            Player.Volume = e.NewValue;
        }

        private async void Playlist_WannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            if (e.NewValue.HasValue) await UpdateCurrentSong();
        }

        private void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            CheckCurrentSong((IPlaylist)sender);
        }

        private static void CheckCurrentSong()
        {
            if (playlist == null) return;

            if (playlist.Songs == null || playlist.Songs.Length == 0) playlist.WannaSong = null;
            else if (!playlist.WannaSong.HasValue || !playlist.Songs.Contains(playlist.WannaSong.Value.Song))
            {
                playlist.WannaSong = RequestSong.Start(playlist.Songs.First());
            }
        }

        private async Task UpdateCurrentSong()
        {
            StopTimer();
            isSetCurrentSong = true;

            IPlaylist currentPlaylist = Service.CurrentPlaylist;
            RequestSong? wannaSong = currentPlaylist?.WannaSong;
            await Player.Set(wannaSong);

            if (currentPlaylist != null && currentPlaylist.WannaSong.Equals(wannaSong))
            {
                currentPlaylist.CurrentSong = wannaSong?.Song;
            }

            isSetCurrentSong = false;

            EnableTimer();
        }

        private void EnableTimer()
        {
            if (!isSetCurrentSong && Service.CurrentPlaylist?.CurrentSong != null &&
                Service.PlayState == PlaybackState.Playing) StartTimer();
            else StopTimer();

            UpdatePosition();
        }

        private void StartTimer()
        {
            timer?.Change(updateInterval, updateInterval);
        }

        private void StopTimer()
        {
            timer?.Change(-1, -1);
        }

        public async Task Continue(Song? currentSong = null)
        {
            if (!currentPlaylistId.HasValue) return;

            Guid playlistId = currentPlaylistId.Value;
            if (loop == LoopType.CurrentSong)
            {
                RequestSong? requestSong = RequestSong.Start(songs.Cast<Song?>().FirstOrDefault(s => s?.Id == currentSongId));
                await playlistsRepo.SendRequestSongChange(playlistId, requestSong);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsHelper.GetNextSong(songs, shuffle, currentSong);

            if (loop == LoopType.StopCurrentSong)
            {
                await libraryRepo.SendPlayStateChange(PlaybackState.Paused);
                await SendCurrentPlaylistsAndRequestSong(currentPlaylistId, newCurrentSong);
            }
            else if (loop == LoopType.CurrentPlaylist || !overflow)
            {
                await SendCurrentPlaylistsAndRequestSong(currentPlaylistId, newCurrentSong);
            }
            else if (loop == LoopType.Next)
            {
                Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>.Next(currentPlaylistId).next;
                await SendCurrentPlaylistsAndRequestSong(newCurrentPlaylistId, newCurrentSong);
            }
            else if (loop == LoopType.Stop)
            {
                await libraryRepo.SendPlayStateChange(PlaybackState.Paused);

                Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>.Next(currentPlaylistId).next;
                await SendCurrentPlaylistsAndRequestSong(newCurrentPlaylistId, newCurrentSong);
            }
        }

        private async Task SendCurrentPlaylistsAndRequestSong(Guid? newCurrentPlaylistId, Song? newCurrentSong)
        {
            if (newCurrentPlaylistId.HasValue)
            {
                await playlistsRepo.SendRequestSongChange(newCurrentPlaylistId.Value, RequestSong.Start(newCurrentSong));
            }

            await libraryRepo.SendCurrentPlaylistIdChange(newCurrentPlaylistId);
        }

        public Task Dispose()
        {
            Player.MediaOpened -= Player_MediaOpened;
            Player.MediaFailed -= Player_MediaFailed;
            Player.MediaEnded -= Player_MediaEnded;
            Unsubscribe(libraryRepo);
            Unsubscribe(playlistsRepo);

            timer.Dispose();
            Player.Stop();
            Player.Dispose();

            return Task.CompletedTask;
        }
    }
}
