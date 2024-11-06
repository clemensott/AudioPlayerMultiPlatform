using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
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
        private TimeSpan position, duration;
        private RequestSong? requestSong;
        private Guid? currentSongId;
        private ICollection<Song> songs;

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
            SubsribePlayer();
            SubscribeLibraryRepo();
            SubscribePlaylistsRepo();

            Library library = await libraryRepo.GetLibrary();
            playlistIds = library.Playlists.Select(p => p.Id).ToList();

            Player.PlayState = library.PlayState;
            await ChangeCurrentPlaylist(library.CurrentPlaylistId);

            await libraryRepo.SendVolumeChange(Player.Volume);
        }

        public async Task Stop()
        {
            UnsubsribePlayer();
            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();

            await SendCurrentState();
            await Player.Stop();
        }

        private async Task SendCurrentState()
        {
            (Guid? playlistId, RequestSong? state) currentState = GetCurrentStateAsRequestSong();
            if (currentState.playlistId.HasValue)
            {
                await playlistsRepo.SendRequestSongChange(currentState.playlistId.Value, currentState.state);
            }
        }

        private (Guid? playlistId, RequestSong? state) GetCurrentStateAsRequestSong()
        {
            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                RequestSong? currentState = requestSong.TryHasValue(out RequestSong request)
                    ? (RequestSong?)RequestSong.Get(request.Song, position, duration)
                    : null;
                return (playlistId, currentState);
            }

            return (null, null);
        }

        private async Task ChangeCurrentPlaylist(Guid? newCurrentPlaylistId, bool saveCurrentState = false)
        {
            if (currentPlaylistId == newCurrentPlaylistId) return;

            (Guid? playlistId, RequestSong? state) currentState = saveCurrentState ? GetCurrentStateAsRequestSong() : (null, null);

            currentPlaylistId = newCurrentPlaylistId;
            if (newCurrentPlaylistId.HasValue)
            {
                var currentPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                shuffle = currentPlaylist.Shuffle;
                loop = currentPlaylist.Loop;
                requestSong = currentPlaylist.RequestSong;
                songs = currentPlaylist.Songs;
            }
            else
            {
                requestSong = null;
                songs = new Song[0];
            }

            // if a request song has been set by CheckRequestedSong then UpdateCurrentSong got called already
            if (!await CheckRequestedSong()) await UpdateCurrentSong();

            if (currentState.playlistId.HasValue)
            {
                await playlistsRepo.SendRequestSongChange(currentState.playlistId.Value, currentState.state);
            }
        }

        private async void Timer_Elapsed(object state)
        {
            await dispatcher.InvokeDispatcher(UpdatePosition);
        }

        private async Task UpdatePosition()
        {
            try
            {
                if (!Player.Source.HasValue ||
                    Player.Source?.Id != currentSongId ||
                    !currentPlaylistId.TryHasValue(out Guid playlistId)) return;

                TimeSpan currentPosition = Player.Position;
                if (currentPosition.Seconds != position.Seconds)
                {
                    position = currentPosition;
                    await playlistsRepo.SendPositionChange(playlistId, position);
                }

                TimeSpan currentDuration = Player.Duration;
                if (currentDuration != duration)
                {
                    duration = currentDuration;
                    await playlistsRepo.SendDurationChange(playlistId, duration);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private void SubsribePlayer()
        {
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaFailed += Player_MediaFailed;
            Player.MediaEnded += Player_MediaEnded;
        }

        private void UnsubsribePlayer()
        {
            Player.MediaOpened -= Player_MediaOpened;
            Player.MediaFailed -= Player_MediaFailed;
            Player.MediaEnded -= Player_MediaEnded;
        }

        private async void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            errorCount = 0;

            await EnableTimer();
        }

        private async void Player_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            StopTimer();
            if (++errorCount < 10) await dispatcher.InvokeDispatcher(() => Continue(e.Song));
        }

        private async void Player_MediaEnded(object sender, MediaEndedEventArgs e)
        {
            Logs.Log($"Player_MediaEnded: {e.Song?.FullPath}");
            StopTimer();
            await dispatcher.InvokeDispatcher(() => Continue(e.Song));
        }


        private void SubscribeLibraryRepo()
        {
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            await ChangeCurrentPlaylist(e.NewValue, sender != this);
        }

        private async void OnPlayStateChange(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            Player.PlayState = e.NewValue;

            await dispatcher.InvokeDispatcher(() => EnableTimer());

            if (e.NewValue == PlaybackState.Paused) await SendCurrentState();
        }

        private void OnVolumeChange(object sender, AudioLibraryChangeArgs<double> e)
        {
            Player.Volume = (float)e.NewValue;
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist += OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist += OnRemovePlaylist;
            playlistsRepo.OnShuffleChange += OnShuffleChange;
            playlistsRepo.OnLoopChange += OnLoopChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnCurrentSongIdChange += OnCurrentSongIdChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist -= OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist -= OnRemovePlaylist;
            playlistsRepo.OnShuffleChange -= OnShuffleChange;
            playlistsRepo.OnLoopChange -= OnLoopChange;
            playlistsRepo.OnPlaybackRateChange -= OnPlaybackRateChange;
            playlistsRepo.OnPositionChange -= OnPositionChange;
            playlistsRepo.OnCurrentSongIdChange -= OnCurrentSongIdChange;
            playlistsRepo.OnRequestSongChange -= OnRequestSongChange;
            playlistsRepo.OnSongsChange -= OnSongsChange;
        }

        private void OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            playlistIds.Insert(e.Index ?? playlistIds.Count, e.Playlist.Id);
        }

        private void OnRemovePlaylist(object sender, RemovePlaylistArgs e)
        {
            playlistIds.Remove(e.Id);
        }

        private void OnShuffleChange(object sender, PlaylistChangeArgs<OrderType> e)
        {
            if (e.Id == currentPlaylistId) shuffle = e.NewValue;
        }

        private void OnLoopChange(object sender, PlaylistChangeArgs<LoopType> e)
        {
            if (e.Id == currentPlaylistId) loop = e.NewValue;
        }

        private void OnPlaybackRateChange(object sender, PlaylistChangeArgs<double> e)
        {
            Player.PlaybackRate = e.NewValue;
        }

        private void OnPositionChange(object sender, PlaylistChangeArgs<TimeSpan> e)
        {
            if (e.Id == currentPlaylistId) position = e.NewValue;
        }

        private void OnCurrentSongIdChange(object sender, PlaylistChangeArgs<Guid?> e)
        {
            if (currentPlaylistId == e.Id) currentSongId = e.NewValue;
        }

        private async void OnRequestSongChange(object sender, PlaylistChangeArgs<RequestSong?> e)
        {
            if (e.Id == currentPlaylistId)
            {
                requestSong = e.NewValue;
                await UpdateCurrentSong();
            }
        }

        private async void OnSongsChange(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            if (e.Id == currentPlaylistId)
            {
                songs = e.NewValue;
                await CheckRequestedSong();
            }
        }

        /// <summary>
        /// Checks if playlists has an requested song and if not sets first song
        /// </summary>
        /// <returns>Has set an requested song</returns>
        private async Task<bool> CheckRequestedSong()
        {
            if (!currentPlaylistId.TryHasValue(out Guid playlistId)) return false;

            if (songs == null || songs.Count == 0)
            {
                await playlistsRepo.SendRequestSongChange(playlistId, null);
                return true;
            }
            else if (!requestSong.HasValue || !songs.Contains(requestSong.Value.Song))
            {
                await playlistsRepo.SendRequestSongChange(playlistId, RequestSong.Start(songs.First()));
                return true;
            }

            return false;
        }

        private async Task UpdateCurrentSong()
        {
            StopTimer();
            isSetCurrentSong = true;

            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                RequestSong? setRequestSong = requestSong;
                await Player.Set(setRequestSong);

                if (requestSong.Equals(setRequestSong))
                {
                    await playlistsRepo.SendCurrentSongIdChange(playlistId, setRequestSong?.Song.Id);
                }
            }
            else await Player.Set(null);

            isSetCurrentSong = false;

            await EnableTimer();
        }

        private async Task EnableTimer()
        {
            if (!isSetCurrentSong && currentPlaylistId.HasValue &&
                Player.PlayState == PlaybackState.Playing) StartTimer();
            else StopTimer();

            await dispatcher.InvokeDispatcher(UpdatePosition);
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
            if (!currentPlaylistId.TryHasValue(out Guid playlistId)) return;

            if (loop == LoopType.CurrentSong)
            {
                RequestSong? requestSong = RequestSong.Start(currentSong);
                await playlistsRepo.SendRequestSongChange(playlistId, requestSong);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsHelper.GetNextSong(songs, shuffle, currentSong);
            RequestSong? newRequestSong = RequestSong.Start(newCurrentSong);

            if (loop == LoopType.StopCurrentSong)
            {
                await libraryRepo.SendPlayStateChange(PlaybackState.Paused);
            }
            else if (overflow)
            {
                if (loop == LoopType.Next)
                {
                    Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>().Next(playlistId).next;
                    await libraryRepo.SendCurrentPlaylistIdChange(newCurrentPlaylistId);
                }
                else if (loop == LoopType.Stop)
                {
                    await libraryRepo.SendPlayStateChange(PlaybackState.Paused);

                    Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>().Next(playlistId).next;
                    await libraryRepo.SendCurrentPlaylistIdChange(newCurrentPlaylistId);
                }
            }

            await playlistsRepo.SendRequestSongChange(playlistId, newRequestSong);
        }

        public async Task Dispose()
        {
            await Stop();

            timer.Dispose();
            Player.Dispose();
        }
    }
}
