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

        private readonly IServicedLibraryRepo libraryRepo;
        private readonly IServicedPlaylistsRepo playlistsRepo;
        private readonly IInvokeDispatcherService dispatcher;
        private bool isSetCurrentSong;
        private int errorCount;
        private readonly Timer timer;

        private Guid? currentPlaylistId;
        private IList<Guid> playlistIds;
        private OrderType shuffle;
        private LoopType loop;
        private TimeSpan position;
        private RequestSong? requestSong;
        private Guid? currentSongId;
        private ICollection<Song> songs;

        public IPlayer Player { get; }

        public AudioPlayerService(IServicedLibraryRepo libraryRepo, IServicedPlaylistsRepo playlistsRepo, IPlayer player, IInvokeDispatcherService dispatcher)
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

            await ChangeCurrentPlaylist(library.CurrentPlaylistId);

            Player.PlayState = library.PlayState;
            await libraryRepo.SendVolumeChange(Player.Volume);
        }

        public async Task Stop()
        {
            UnsubsribePlayer();
            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();

            if (currentPlaylistId.HasValue && requestSong.HasValue)
            {
                await playlistsRepo.SendRequestSongChange(currentPlaylistId.Value, RequestSong.Get(requestSong.Value.Song, position));
            }

            await Player.Stop();
        }

        private async Task ChangeCurrentPlaylist(Guid? newCurrentPlaylistId)
        {
            if (currentPlaylistId == newCurrentPlaylistId) return;

            if (currentPlaylistId.TryHasValue(out Guid oldPlaylistId))
            {
                var oldPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                Song? currentSong = oldPlaylist.Songs.Cast<Song?>().FirstOrDefault(s => s?.Id == oldPlaylist.CurrentSongId);
                await playlistsRepo.SendRequestSongChange(oldPlaylistId, RequestSong.Get(currentSong, oldPlaylist.Position, oldPlaylist.Duration));
            }

            currentPlaylistId = newCurrentPlaylistId;
            if (newCurrentPlaylistId.HasValue)
            {
                var currentPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                shuffle = currentPlaylist.Shuffle;
                loop = currentPlaylist.Loop;
                requestSong = currentPlaylist.RequestSong;
                songs = currentPlaylist.Songs;
            }
            else songs = new Song[0];

            // if a request song has been set by CheckRequestedSong then UpdateCurrentSong got called already
            if (!await CheckRequestedSong()) await UpdateCurrentSong();
        }

        private async Task CheckUpdateCurrentSong()
        {
            if (Player.Source.HasValue ^ requestSong.HasValue)
            {
                await UpdateCurrentSong();
            }
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
                    Player.Source?.Id != currentSongId ||
                    position.Seconds == Player.Position.Seconds ||
                    !currentPlaylistId.HasValue) return;

                Guid playlistId = currentPlaylistId.Value;
                position = Player.Position;
                playlistsRepo.SendPositionChange(playlistId, position);
                playlistsRepo.SendDurationChange(playlistId, Player.Duration);
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

            EnableTimer();

            if (currentPlaylistId.HasValue && requestSong?.Song == e.Source)
            {
                Guid playlistId = currentPlaylistId.Value;

                await playlistsRepo.SendCurrentSongIdChange(playlistId, e.Source.Id);
                await playlistsRepo.SendPositionChange(playlistId, e.Position);
                await playlistsRepo.SendDurationChange(playlistId, e.Duration);
            }
        }

        private async void Player_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            StopTimer();
            if (++errorCount < 10) await Continue(e.Song);
        }

        private async void Player_MediaEnded(object sender, MediaEndedEventArgs e)
        {
            Logs.Log($"Player_MediaEnded: {e.Song?.FullPath}");
            StopTimer();
            await Continue(e.Song);
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
            await ChangeCurrentPlaylist(e.NewValue);
        }

        private void OnPlayStateChange(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            Player.PlayState = e.NewValue;

            EnableTimer();
        }

        private void OnVolumeChange(object sender, AudioLibraryChangeArgs<double> e)
        {
            Player.Volume = (float)e.NewValue;
        }

        private void OnPlaybackRateChange(object sender, PlaylistChangeArgs<double> e)
        {
            Player.PlaybackRate = e.NewValue;
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist += OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist += OnRemovePlaylist;
            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnCurrentSongIdChange += OnCurrentSongIdChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.OnInsertPlaylist -= OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist -= OnRemovePlaylist;
            playlistsRepo.OnPositionChange -= OnPositionChange;
            playlistsRepo.OnPlaybackRateChange -= OnPlaybackRateChange;
            playlistsRepo.OnCurrentSongIdChange -= OnCurrentSongIdChange;
            playlistsRepo.OnRequestSongChange -= OnRequestSongChange;
            playlistsRepo.OnSongsChange -= OnSongsChange;
        }

        private void OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            if (e.Index.HasValue) playlistIds.Insert(e.Index.Value, e.Playlist.Id);
            else playlistIds.Add(e.Playlist.Id);
        }

        private void OnRemovePlaylist(object sender, RemovePlaylistArgs e)
        {
            playlistIds.Remove(e.Id);
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

            EnableTimer();
        }

        private void EnableTimer()
        {
            if (!isSetCurrentSong && currentPlaylistId.HasValue &&
                Player.PlayState == PlaybackState.Playing) StartTimer();
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
            if (!currentPlaylistId.TryHasValue(out Guid playlistId)) return;

            if (loop == LoopType.CurrentSong)
            {
                RequestSong? requestSong = RequestSong.Start(songs.Cast<Song?>().FirstOrDefault(s => s?.Id == currentSongId));
                await playlistsRepo.SendRequestSongChange(playlistId, requestSong);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsHelper.GetNextSong(songs, shuffle, currentSong);

            if (loop == LoopType.StopCurrentSong)
            {
                await SendPlayStateChange(PlaybackState.Paused);
                await SendCurrentPlaylistsAndRequestSong(playlistId, newCurrentSong);
            }
            else if (loop == LoopType.CurrentPlaylist || !overflow)
            {
                await SendCurrentPlaylistsAndRequestSong(playlistId, newCurrentSong);
            }
            else if (loop == LoopType.Next)
            {
                Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>().Next(playlistId).next;
                await SendCurrentPlaylistsAndRequestSong(newCurrentPlaylistId, newCurrentSong);
            }
            else if (loop == LoopType.Stop)
            {
                await SendPlayStateChange(PlaybackState.Paused);

                Guid? newCurrentPlaylistId = playlistIds.Cast<Guid?>().Next(playlistId).next;
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

        private Task SendPlayStateChange(PlaybackState playState)
        {
            Player.PlayState = playState;
            return libraryRepo.SendPlayStateChange(playState);
        }

        public async Task Dispose()
        {
            await Stop();

            await libraryRepo.Dispose();
            await playlistsRepo.Dispose();
            timer.Dispose();
            Player.Dispose();
        }
    }
}
