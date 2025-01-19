using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.GenericEventArgs;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private const int minPreLoadMediaSources = 5;

        private int setSourceCount = 0;
        private PlaybackState playState;
        private RequestSong? request;
        private RequestSong setRequest;
        private ICollection<Song> songs;
        private readonly IDictionary<Song, int> songsIndexes;
        private readonly IDictionary<MediaPlaybackItem, Song> mediaPlaybackItemDict;
        private readonly MediaPlaybackList mediaPlaybackList;
        private readonly SemaphoreSlim sem;
        private readonly MediaPlayer player;

        public event EventHandler<MediaOpenedEventArgs> MediaOpened;
        public event EventHandler<MediaFailedEventArgs> MediaFailed;
        public event EventHandler<MediaEndedEventArgs> MediaEnded;
        public event EventHandler<HandledEventArgs> NextPressed;
        public event EventHandler<HandledEventArgs> PreviousPressed;
        public event EventHandler<ValueChangedEventArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                PlaybackState oldState = playState;
                playState = value;

                HandlePlayStateChange();

                if (oldState != value)
                {
                    PlayStateChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackState>(oldState, value));
                }
            }
        }

        public TimeSpan Position => player.PlaybackSession.Position;

        public TimeSpan Duration => player.PlaybackSession.NaturalDuration;

        public Song? Source { get; private set; }

        public float Volume { get => (float)player.Volume; set => player.Volume = value; }

        public double PlaybackRate
        {
            get => player.PlaybackSession.PlaybackRate;
            set => player.PlaybackSession.PlaybackRate = value;
        }

        public SystemMediaTransportControls SMTC => player.SystemMediaTransportControls;

        public Player()
        {
            sem = new SemaphoreSlim(1);
            player = new MediaPlayer();
            player.MediaOpened += Player_MediaOpened;
            player.MediaFailed += Player_MediaFailed;
            player.MediaEnded += Player_MediaEnded;
            player.VolumeChanged += Player_VolumeChanged;

            player.CommandManager.IsEnabled = true;
            player.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.PlayReceived += CommandManager_PlayReceived;
            player.CommandManager.PauseReceived += CommandManager_PauseReceived;

            songs = new Song[0];
            songsIndexes = new Dictionary<Song, int>();
            mediaPlaybackList = new MediaPlaybackList();
            mediaPlaybackItemDict = new Dictionary<MediaPlaybackItem, Song>();
            mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            mediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
            mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;

            player.Source = mediaPlaybackList;
        }

        private void Player_VolumeChanged(MediaPlayer sender, object args)
        {
            VolumeChanged?.Invoke(this, new ValueChangedEventArgs<float>(-1, Volume));
        }

        private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
        {
            PlayState = PlaybackState.Playing;
            args.Handled = true;
        }

        private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
        {
            PlayState = PlaybackState.Paused;
            args.Handled = true;
        }

        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
            //Source = setRequest.Song;
            //AudioPlayerBackend.Logs.Log("Player.Player_MediaOpened1", setRequest.Song.FullPath, setSourceCount);
            //if (setRequest.Duration == sender.PlaybackSession.NaturalDuration)
            //{
            //    sender.PlaybackSession.Position = setRequest.Position;
            //}

            //MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, setRequest.Song));
            //ExecutePlayState();
            //sem.Release();
        }

        private void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.MediaPlaybackList_ItemOpened1", setRequest.Song.FullPath, setSourceCount);
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaFailed1", setRequest.Song.FullPath, setSourceCount);
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequest.Song, args.ExtendedErrorCode));
            sem.Release();
        }

        private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.MediaPlaybackList_ItemFailed1", setRequest.Song.FullPath, setSourceCount);
            Source = null;
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequest.Song, args.Error.ExtendedError));
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaEnded1", Source?.FullPath, setSourceCount);
            MediaEnded?.Invoke(this, new MediaEndedEventArgs(Source));
        }

        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (args.Reason == MediaPlaybackItemChangedReason.InitialItem) return;
            if (args.NewItem != null)
            {
                Song newSong = mediaPlaybackItemDict[args.NewItem];
                Source = newSong;
                MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, newSong));
            }
            else Source = null;

            await sem.WaitAsync();
            try
            {
                if (args.NewItem != null)
                {
                    Song newSong = mediaPlaybackItemDict[args.NewItem];
                    await UpsertSongWithPreloadInPlaybackList(newSong, TimeSpan.Zero);
                }

                if (args.OldItem != null && args.OldItem.StartTime != TimeSpan.Zero)
                {
                    Song oldSong = mediaPlaybackItemDict[args.OldItem];
                    StorageFile file = await StorageFile.GetFileFromPathAsync(oldSong.FullPath);
                    MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
                    mediaPlaybackItemDict[mediaPlaybackItem] = oldSong;
                    mediaPlaybackItemDict.Remove(args.OldItem);
                    int index = mediaPlaybackList.Items.IndexOf(args.OldItem);
                    mediaPlaybackList.Items[index] = mediaPlaybackItem;
                }
            }
            finally
            {
                sem.Release();
            }
        }

        public Task<(TimeSpan position, TimeSpan duration)> GetTimesSafe()
        {
            return Task.FromResult((Position, Duration));
        }

        public async Task SetSongs(ICollection<Song> songs)
        {
            await sem.WaitAsync();
            try
            {
                this.songs = songs ?? new Song[0];
                songsIndexes.Clear();

                int index = 0;
                foreach (Song song in songs)
                {
                    songsIndexes[song] = index++;
                }

                mediaPlaybackList.Items.Clear();
                mediaPlaybackItemDict.Clear();
            }
            finally
            {
                sem.Release();
            }
        }

        public Task Set(RequestSong? request)
        {
            return request.HasValue ? Set(request.Value) : Stop();
        }

        private async Task Set(RequestSong request)
        {
            this.request = request;

            await sem.WaitAsync();

            bool release = true;
            try
            {
                if (!this.request.Equals(request)) return;

                if (Source.HasValue && request.Song.FullPath == Source?.FullPath)
                {
                    if (!request.ContinuePlayback && request.Position != Position)
                    {
                        player.PlaybackSession.Position = request.Position;
                    }
                    setSourceCount++;
                    Source = request.Song;
                    ExecutePlayState();
                    return;
                }
                release = false;
            }
            catch (Exception e)
            {
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
            }
            finally
            {
                if (release) sem.Release();
            }

            try
            {
                //setRequest = request;
                //AudioPlayerBackend.Logs.Log("Player.Set5", request.Song.FullPath, request.ContinuePlayback, setSourceCount);
                //StorageFile file = await StorageFile.GetFileFromPathAsync(request.Song.FullPath);
                //AudioPlayerBackend.Logs.Log("Player.Set6", file.Path, setSourceCount);
                //MediaSource mediaSource = MediaSource.CreateFromStorageFile(file);
                //MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(mediaSource, request.Position);
                //mediaPlaybackItem.AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music;
                //mediaPlaybackList.Items.Clear();
                //mediaPlaybackList.Items.Add(mediaPlaybackItem);

                MediaPlaybackItem item = await UpsertSongWithPreloadInPlaybackList(request.Song, request.Position);
                int index = mediaPlaybackList.Items.IndexOf(item);
                mediaPlaybackList.MoveTo((uint)index);

                //await SMTC.DisplayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                //if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Title))
                //{
                //    SMTC.DisplayUpdater.MusicProperties.Title = request.Song.Title ?? string.Empty;
                //}
                //if (string.IsNullOrWhiteSpace(SMTC.DisplayUpdater.MusicProperties.Artist))
                //{
                //    SMTC.DisplayUpdater.MusicProperties.Artist = request.Song.Artist ?? string.Empty;
                //}
                //SMTC.DisplayUpdater.Update();
            }
            catch (Exception e)
            {
                Source = null;
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
            }
            finally
            {
                sem.Release();
            }
        }

        private async Task<MediaPlaybackItem> UpsertSongWithPreloadInPlaybackList(Song upsertSong, TimeSpan startTime)
        {
            MediaPlaybackItem result = null;
            int upsertSongIndex = songsIndexes[upsertSong];

            // index of current search position in mediaPlaybackList
            // makes it faster to find next spot to insert into because songs are ordered and next song is always the next position
            int listIndex = 0;

            // this loop might try to insert a song multiple times
            // when the total count of songs is smaller than the amount of songs that are preloaded
            // this is not a problem because it checks if a song is already in the list
            // it's "just" suboptimal for the performance
            for (int i = upsertSongIndex - minPreLoadMediaSources; i < upsertSongIndex + minPreLoadMediaSources; i++)
            {
                Song insertSong = songs.ElementAtCycle(i);
                int songsIndex = songsIndexes[insertSong];
                int listInsertIndex;
                if (mediaPlaybackList.Items.Count == 0)
                {
                    listInsertIndex = 0;
                    listIndex = 0;
                }
                else if (songsIndex < songsIndexes[mediaPlaybackItemDict[mediaPlaybackList.Items[0]]])
                {
                    listInsertIndex = 0;
                    listIndex = 0;
                }
                else
                {
                    while (true)
                    {
                        listIndex = listIndex % mediaPlaybackList.Items.Count;

                        Song listIndexSong = mediaPlaybackItemDict[mediaPlaybackList.Items[listIndex]];
                        if (listIndexSong == insertSong)
                        {
                            if (upsertSong == insertSong) result = mediaPlaybackList.Items[listIndex];

                            // song ist already in list
                            listIndex++;
                            listInsertIndex = -1;
                            break;
                        }

                        int listItemIndex = songsIndexes[listIndexSong];
                        int nextListItemIndex = listIndex + 1 < mediaPlaybackList.Items.Count
                            ? songsIndexes[mediaPlaybackItemDict[mediaPlaybackList.Items[listIndex + 1]]]
                            : int.MaxValue;

                        if (listItemIndex < songsIndex && nextListItemIndex > songsIndex)
                        {
                            listIndex++;
                            listInsertIndex = listIndex;
                            break;
                        }

                        listIndex++;
                    }
                }

                if (listInsertIndex != -1)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(insertSong.FullPath);
                    MediaSource mediaSource = MediaSource.CreateFromStorageFile(file);
                    TimeSpan itemStartTime = insertSong == upsertSong ? startTime : TimeSpan.Zero;
                    MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(mediaSource, itemStartTime);
                    mediaPlaybackItem.AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music;
                    mediaPlaybackItemDict[mediaPlaybackItem] = insertSong;
                    mediaPlaybackList.Items.Insert(listInsertIndex, mediaPlaybackItem);

                    if (insertSong == upsertSong) result = mediaPlaybackItem;
                }
            }

            return result;
        }

        public async Task Stop()
        {
            request = null;
            await sem.WaitAsync();

            try
            {
                mediaPlaybackList.Items.Clear();
                mediaPlaybackItemDict.Clear();
                Source = null;
            }
            finally
            {
                sem.Release();
            }
        }

        private async void HandlePlayStateChange()
        {
            await sem.WaitAsync();

            try
            {
                ExecutePlayState();
            }
            finally
            {
                sem.Release();
            }
        }

        public void ExecutePlayState()
        {
            switch (PlayState)
            {
                case PlaybackState.Playing:
                    player.Play();
                    break;

                case PlaybackState.Paused:
                    player.Pause();
                    break;
            }
        }

        public void Dispose()
        {
            AudioPlayerBackend.Logs.Log("Player.Dispose");
            player.MediaOpened -= Player_MediaOpened;
            player.MediaFailed -= Player_MediaFailed;
            player.MediaEnded -= Player_MediaEnded;

            Source = null;

            player.Dispose();
        }
    }
}
