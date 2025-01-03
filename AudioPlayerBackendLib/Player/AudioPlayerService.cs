﻿using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
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
        private bool isUpdatingPosition;
        private int isSetCurrentSongCount, errorCount, updatePositionCount;
        private readonly Timer timer;

        private Guid? currentPlaylistId;
        private IList<Guid> playlistIds;
        private OrderType shuffle;
        private LoopType loop;
        private SongRequest? request;
        private ICollection<Song> songs;

        public IPlayer Player { get; }

        public AudioPlayerService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo, IPlayer player)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            Player = player;

            timer = new Timer(Timer_Elapsed, null, -1, updateInterval);
        }

        public async Task Start()
        {
            Logs.Log("AudioPlayerService.Start1");
            SubsribePlayer();
            SubscribeLibraryRepo();
            SubscribePlaylistsRepo();
            Logs.Log("AudioPlayerService.Start2");

            Library library = await libraryRepo.GetLibrary();
            Logs.Log("AudioPlayerService.Start3");
            playlistIds = library.Playlists.Select(p => p.Id).ToList();

            Player.PlayState = library.PlayState;
            Logs.Log("AudioPlayerService.Start4");
            await ChangeCurrentPlaylist(library.CurrentPlaylistId);
            Logs.Log("AudioPlayerService.Start5");

            await libraryRepo.SetVolume(Player.Volume);
            Logs.Log("AudioPlayerService.Start6");
        }

        public async Task Stop()
        {
            UnsubsribePlayer();
            UnsubscribeLibraryRepo();
            UnsubscribePlaylistsRepo();

            await Player.Stop();
        }

        private async Task ChangeCurrentPlaylist(Guid? newCurrentPlaylistId, bool saveCurrentState = false)
        {
            Logs.Log("AudioPlayerService.ChangeCurrentPlaylist1");
            if (currentPlaylistId == newCurrentPlaylistId) return;

            currentPlaylistId = newCurrentPlaylistId;
            Logs.Log("AudioPlayerService.ChangeCurrentPlaylist2");
            if (newCurrentPlaylistId.HasValue)
            {
                Playlist currentPlaylist = await playlistsRepo.GetPlaylist(newCurrentPlaylistId.Value);
                Logs.Log("AudioPlayerService.ChangeCurrentPlaylist3");
                shuffle = currentPlaylist.Shuffle;
                loop = currentPlaylist.Loop;
                request = currentPlaylist.CurrentSongRequest;
                songs = currentPlaylist.Songs;
            }
            else
            {
                request = null;
                songs = new Song[0];
            }

            Logs.Log("AudioPlayerService.ChangeCurrentPlaylist4");
            // if a request song has been set by CheckCurrentSongRequest then UpdateCurrentSong got called already
            if (!await CheckCurrentSongRequest())
            {
                Logs.Log("AudioPlayerService.ChangeCurrentPlaylist5");
                await UpdateCurrentSong();
            }
            Logs.Log("AudioPlayerService.ChangeCurrentPlaylist6");
        }

        private async void Timer_Elapsed(object state)
        {
            await UpdatePosition();
        }

        private async Task UpdatePosition()
        {
            try
            {
                if (isUpdatingPosition
                    || isSetCurrentSongCount > 0
                    || !Player.Source.TryHasValue(out Song currentSong)
                    || currentSong.Id != request?.Id
                    || !currentPlaylistId.TryHasValue(out Guid playlistId)) return;
                isUpdatingPosition = true;

                try
                {
                    (TimeSpan position, TimeSpan duration) current = await Player.GetTimesSafe();

                    if (current.position.Seconds != request?.Position.Seconds || current.duration != request?.Duration)
                    {
                        SongRequest newSongRequest = SongRequest.Get(currentSong.Id, current.position, current.duration, true);
                        updatePositionCount++;
                        await playlistsRepo.SetCurrentSongRequest(playlistId, newSongRequest);
                    }
                }
                finally
                {
                    isUpdatingPosition = false;
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
            Player.NextPressed += Player_NextPressed;
            Player.PreviousPressed += Player_PreviousPressed;
            Player.PlayStateChanged += Player_PlayStateChanged;
        }

        private void UnsubsribePlayer()
        {
            Player.MediaOpened -= Player_MediaOpened;
            Player.MediaFailed -= Player_MediaFailed;
            Player.MediaEnded -= Player_MediaEnded;
            Player.NextPressed -= Player_NextPressed;
            Player.PreviousPressed -= Player_PreviousPressed;
            Player.PlayStateChanged -= Player_PlayStateChanged;
        }

        private async void Player_NextPressed(object sender, HandledEventArgs e)
        {
            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                e.Handled = true;

                Song? currentSong = songs.FirstOrDefault(s => s.Id == request?.Id);
                Song? newCurrentSong = SongsHelper.GetNextSong(songs, shuffle, currentSong).song;
                await playlistsRepo.SetCurrentSongRequest(playlistId, SongRequest.Start(newCurrentSong?.Id));
            }
        }

        private async void Player_PreviousPressed(object sender, HandledEventArgs e)
        {
            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                e.Handled = true;

                Song? currentSong = songs.FirstOrDefault(s => s.Id == request?.Id);
                Song? newCurrentSong = SongsHelper.GetPreviousSong(songs, shuffle, currentSong).song;
                await playlistsRepo.SetCurrentSongRequest(playlistId, SongRequest.Start(newCurrentSong?.Id));
            }
        }

        private async void Player_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                await libraryRepo.SetPlayState(e.NewValue);
            }
        }

        private async void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            Logs.Log("AudioPlayerService.Player_MediaOpened1", e.Source.FullPath, updatePositionCount);
            errorCount = 0;

            await EnableTimer();
            Logs.Log("AudioPlayerService.Player_MediaOpened2", updatePositionCount);
        }

        private async void Player_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            StopTimer();
            if (++errorCount < 10) await Continue(e.Song);
        }

        private async void Player_MediaEnded(object sender, MediaEndedEventArgs e)
        {
            isSetCurrentSongCount++;
            try
            {
                Logs.Log("AudioPlayerService.Player_MediaEnded1", updatePositionCount);
                StopTimer();
                await Continue(e.Song);
                Logs.Log("AudioPlayerService.Player_MediaEnded3", updatePositionCount);
            }
            finally
            {
                isSetCurrentSongCount--;
            }
        }


        private void SubscribeLibraryRepo()
        {
            libraryRepo.CurrentPlaylistIdChanged += OnCurrentPlaylistIdChanged;
            libraryRepo.PlayStateChanged += OnPlayStateChanged;
            libraryRepo.VolumeChanged += OnVolumeChanged;
        }

        private void UnsubscribeLibraryRepo()
        {
            libraryRepo.CurrentPlaylistIdChanged -= OnCurrentPlaylistIdChanged;
            libraryRepo.PlayStateChanged -= OnPlayStateChanged;
            libraryRepo.VolumeChanged -= OnVolumeChanged;
        }

        private async void OnCurrentPlaylistIdChanged(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            await ChangeCurrentPlaylist(e.NewValue, sender != this);
        }

        private async void OnPlayStateChanged(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            Player.PlayState = e.NewValue;

            await EnableTimer();
        }

        private void OnVolumeChanged(object sender, AudioLibraryChangeArgs<double> e)
        {
            Player.Volume = (float)e.NewValue;
        }

        private void SubscribePlaylistsRepo()
        {
            playlistsRepo.InsertedPlaylist += OnInsertedPlaylist;
            playlistsRepo.RemovedPlaylist += OnRemovedPlaylist;
            playlistsRepo.ShuffleChanged += OnShuffleChanged;
            playlistsRepo.LoopChanged += OnLoopChanged;
            playlistsRepo.PlaybackRateChanged += OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged += OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged += OnSongsChanged;
        }

        private void UnsubscribePlaylistsRepo()
        {
            playlistsRepo.InsertedPlaylist -= OnInsertedPlaylist;
            playlistsRepo.RemovedPlaylist -= OnRemovedPlaylist;
            playlistsRepo.ShuffleChanged -= OnShuffleChanged;
            playlistsRepo.LoopChanged -= OnLoopChanged;
            playlistsRepo.PlaybackRateChanged -= OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged -= OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged -= OnSongsChanged;
        }

        private void OnInsertedPlaylist(object sender, InsertPlaylistArgs e)
        {
            playlistIds.Insert(e.Index ?? playlistIds.Count, e.Playlist.Id);
        }

        private void OnRemovedPlaylist(object sender, RemovePlaylistArgs e)
        {
            playlistIds.Remove(e.Id);
        }

        private void OnShuffleChanged(object sender, PlaylistChangeArgs<OrderType> e)
        {
            if (e.Id == currentPlaylistId) shuffle = e.NewValue;
        }

        private void OnLoopChanged(object sender, PlaylistChangeArgs<LoopType> e)
        {
            if (e.Id == currentPlaylistId) loop = e.NewValue;
        }

        private void OnPlaybackRateChanged(object sender, PlaylistChangeArgs<double> e)
        {
            Player.PlaybackRate = e.NewValue;
        }

        private async void OnCurrentSongRequestChanged(object sender, PlaylistChangeArgs<SongRequest?> e)
        {
            if (e.Id == currentPlaylistId)
            {
                request = e.NewValue;
                await UpdateCurrentSong();
            }
        }

        private async void OnSongsChanged(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            if (e.Id == currentPlaylistId)
            {
                songs = e.NewValue;
                await CheckCurrentSongRequest();
            }
        }

        /// <summary>
        /// Checks if playlists has an requested song and if not sets first song
        /// </summary>
        /// <returns>Has set an song request</returns>
        private async Task<bool> CheckCurrentSongRequest()
        {
            if (!currentPlaylistId.TryHasValue(out Guid playlistId)) return false;

            if (songs == null || songs.Count == 0)
            {
                await playlistsRepo.SetCurrentSongRequest(playlistId, null);
                return true;
            }
            else if (!request.HasValue || !songs.Any(s => s.Id == request?.Id))
            {
                await playlistsRepo.SetCurrentSongRequest(playlistId, SongRequest.Start(songs.First().Id));
                return true;
            }

            return false;
        }

        private async Task UpdateCurrentSong()
        {
            isSetCurrentSongCount++;
            StopTimer();

            if (currentPlaylistId.TryHasValue(out Guid playlistId))
            {
                SongRequest? setSongRequest = request;
                Song? song = songs.FirstOrNull(s => s.Id == setSongRequest?.Id);
                if (setSongRequest?.Id != Player.Source?.Id) Logs.Log("AudioPlayerService.UpdateCurrentSong.ChangeSong", song?.FullPath, updatePositionCount);
                RequestSong? requestSong = song.HasValue
                    ? (RequestSong?)new RequestSong(song.Value, setSongRequest.Value.Position,
                        setSongRequest.Value.Duration, setSongRequest.Value.ContinuePlayback)
                    : null;
                await Player.Set(requestSong);
            }
            else await Player.Set(null);

            isSetCurrentSongCount--;

            await EnableTimer();
        }

        private async Task EnableTimer()
        {
            if (isSetCurrentSongCount == 0 && currentPlaylistId.HasValue &&
                Player.PlayState == PlaybackState.Playing) StartTimer();
            else StopTimer();

            await UpdatePosition();
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
            Logs.Log("AudioPlayerService.Continue1", currentSong?.FullPath);

            if (loop == LoopType.CurrentSong)
            {
                Logs.Log("AudioPlayerService.Continue2");
                SongRequest? songRequest = SongRequest.Start(currentSong?.Id);
                await playlistsRepo.SetCurrentSongRequest(playlistId, songRequest);
                return;
            }

            (Song? newCurrentSong, bool overflow) = SongsHelper.GetNextSong(songs, shuffle, currentSong);
            SongRequest? newSongRequest = SongRequest.Start(newCurrentSong?.Id);
            Logs.Log("AudioPlayerService.Continue3", newCurrentSong?.FullPath, overflow);

            if (loop == LoopType.StopCurrentSong)
            {
                Logs.Log("AudioPlayerService.Continue4");
                await libraryRepo.SetPlayState(PlaybackState.Paused);
            }
            else if (overflow)
            {
                Logs.Log("AudioPlayerService.Continue5");
                if (loop == LoopType.Next)
                {
                    Logs.Log("AudioPlayerService.Continue6");
                    Guid newCurrentPlaylistId = playlistIds.Next(playlistId).next;
                    await libraryRepo.SetCurrentPlaylistId(newCurrentPlaylistId);
                }
                else if (loop == LoopType.Stop)
                {
                    Logs.Log("AudioPlayerService.Continue7");
                    await libraryRepo.SetPlayState(PlaybackState.Paused);

                    Guid newCurrentPlaylistId = playlistIds.Next(playlistId).next;
                    await libraryRepo.SetCurrentPlaylistId(newCurrentPlaylistId);
                }
            }

            Logs.Log("AudioPlayerService.Continue8");
            await playlistsRepo.SetCurrentSongRequest(playlistId, newSongRequest);
            Logs.Log("AudioPlayerService.Continue9");
        }

        public async Task Dispose()
        {
            await Stop();

            timer.Dispose();
            Player.Dispose();
        }
    }
}
