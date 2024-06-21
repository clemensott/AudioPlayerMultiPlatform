using AudioPlayerBackend.Audio;
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
        private TimeSpan position;
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
            SubsribePlayer();
            SubscribeLibraryRepo();
            SubscribePlaylistsRepo();

            Library library = await libraryRepo.GetLibrary();
            currentPlaylistId = library.CurrentPlaylistId;
            playlistIds = library.Playlists.Select(p => p.Id).ToArray();

            await ChangeCurrentPlaylist(currentPlaylistId);

            Player.PlayState = library.PlayState;
            await libraryRepo.SendVolumeChange(Player.Volume);
        }

        public async Task Stop()
        {
            UnsubsribePlayer();
            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();

            await Player.Stop();
        }

        private async Task ChangeCurrentPlaylist(Guid? newCurrentPlaylistId)
        {
            if (currentPlaylistId.TryHasValue(out Guid oldPlaylistId))
            {
                var oldPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                Song? currentSong = oldPlaylist.Songs.Cast<Song?>().FirstOrDefault(s => s?.Id == oldPlaylist.CurrentSongId);
                await SendRequestSongChange(oldPlaylistId, RequestSong.Get(currentSong, oldPlaylist.Position, oldPlaylist.Duration));
            }

            currentPlaylistId = newCurrentPlaylistId;
            if (newCurrentPlaylistId.HasValue)
            {
                var currentPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                shuffle = currentPlaylist.Shuffle;
                loop = currentPlaylist.Loop;
                songs = currentPlaylist.Songs;
            }
            else songs = new Song[0];

            await CheckCurrentSong();
            await UpdateCurrentSong();

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
                    currentPlaylistId.HasValue) return;

                Guid playlistId = currentPlaylistId.Value;
                position = Player.Position;
                SendPositionChange(playlistId, position);
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

                await SendCurrentSongIdChange(playlistId, e.Source.Id);
                await SendPositionChange(playlistId, e.Position);
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
            libraryRepo.OnPlaylistsChange += OnPlaylistsChange;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnPlaylistsChange -= OnPlaylistsChange;
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChange<Guid?> e)
        {
            await ChangeCurrentPlaylist(e.NewValue);
        }

        private void OnPlayStateChange(object sender, AudioLibraryChange<PlaybackState> e)
        {
            Player.PlayState = e.NewValue;

            EnableTimer();
        }

        private void OnVolumeChange(object sender, AudioLibraryChange<double> e)
        {
            Player.Volume = (float)e.NewValue;
        }

        private void OnPlaybackRateChange(object sender, PlaylistChange<double> e)
        {
            Player.PlaybackRate = e.NewValue;
        }

        private void OnPlaylistsChange(object sender, AudioLibraryChange<IList<PlaylistInfo>> e)
        {
            playlistIds = e.NewValue.Select(p => p.Id).ToArray();
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
        }

        private void OnPositionChange(object sender, PlaylistChange<TimeSpan> e)
        {
            if (e.Id == currentPlaylistId) position = e.NewValue;
        }

        private async void OnRequestSongChange(object sender, PlaylistChange<RequestSong?> e)
        {
            if (e.Id == currentPlaylistId)
            {
                requestSong = e.NewValue;
                await UpdateCurrentSong();
            }
        }

        private async void OnSongsChange(object sender, PlaylistChange<System.Collections.Generic.IList<Song>> e)
        {
            if (e.Id == currentPlaylistId)
            {
                songs = e.NewValue;
                await CheckCurrentSong();
            }
        }

        private async Task CheckCurrentSong()
        {
            if (!currentPlaylistId.TryHasValue(out Guid playlistId)) return;

            if (songs == null || songs.Count == 0) await SendRequestSongChange(playlistId, null);
            else if (!requestSong.HasValue || !songs.Contains(requestSong.Value.Song))
            {
                await SendRequestSongChange(playlistId, RequestSong.Start(songs.First()));
            }
        }

        private async Task UpdateCurrentSong()
        {
            StopTimer();
            isSetCurrentSong = true;

            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                RequestSong? setRequestSong = requestSong;

                if (requestSong.Equals(setRequestSong))
                {
                    await SendCurrentSongIdChange(playlistId, setRequestSong?.Song.Id);
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
                await SendRequestSongChange(playlistId, requestSong);
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
                await SendRequestSongChange(newCurrentPlaylistId.Value, RequestSong.Start(newCurrentSong));
            }

            await SendCurrentPlaylistIdChange(newCurrentPlaylistId);
        }

        private Task SendPlayStateChange(PlaybackState playState)
        {
            Player.PlayState = playState;
            return libraryRepo.SendPlayStateChange(playState);
        }

        private async Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            await libraryRepo.SendCurrentPlaylistIdChange(currentPlaylistId);
            await ChangeCurrentPlaylist(currentPlaylistId);
        }

        private Task SendPositionChange(Guid playlistId, TimeSpan position)
        {
            if (currentPlaylistId == playlistId) this.position = position;
            return playlistsRepo.SendPositionChange(playlistId, position);
        }

        private async Task SendRequestSongChange(Guid playlistId, RequestSong? requestSong)
        {
            await playlistsRepo.SendRequestSongChange(playlistId, requestSong);
            if (playlistId == currentPlaylistId)
            {
                this.requestSong = requestSong;
                await UpdateCurrentSong();
            }
        }

        private Task SendCurrentSongIdChange(Guid playlistId, Guid? currentSongId)
        {
            if (currentPlaylistId == playlistId) this.currentSongId = currentSongId;
            return playlistsRepo.SendCurrentSongIdChange(playlistId, currentPlaylistId);
        }

        public async Task Dispose()
        {
            await Stop();

            timer.Dispose();
            Player.Dispose();
        }
    }
}
