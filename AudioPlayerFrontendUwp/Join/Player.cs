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
        private RequestSong? request, setRequest;
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

        public Song? Source
        {
            get => mediaPlaybackList.CurrentItem != null
                && mediaPlaybackItemDict.TryGetValue(mediaPlaybackList.CurrentItem, out Song song) ? (Song?)song : null;
        }

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
            Song song = mediaPlaybackItemDict[args.Item];
            AudioPlayerBackend.Logs.Log("Player.MediaPlaybackList_ItemOpened1", song.FullPath);
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaFailed1", setRequest?.Song.FullPath, setSourceCount);
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequest?.Song, args.ExtendedErrorCode));
            sem.Release();
        }

        private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            AudioPlayerBackend.Logs.Log("Player.MediaPlaybackList_ItemFailed1", setRequest?.Song.FullPath, setSourceCount);
            MediaFailed?.Invoke(this, new MediaFailedEventArgs(setRequest?.Song, args.Error.ExtendedError));
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            AudioPlayerBackend.Logs.Log("Player.Player_MediaEnded1", Source?.FullPath, setSourceCount);
            MediaEnded?.Invoke(this, new MediaEndedEventArgs(Source));
        }

        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (args.NewItem != null)
            {
                Song newSong = mediaPlaybackItemDict[args.NewItem];
                if (newSong == setRequest?.Song && setRequest?.Position > TimeSpan.Zero)
                {
                    player.PlaybackSession.Position = setRequest.Value.Position;
                }

                MediaOpened?.Invoke(this, new MediaOpenedEventArgs(Position, Duration, newSong));
            }

            await sem.WaitAsync();
            try
            {
                if (args.NewItem != null)
                {
                    Song newSong = mediaPlaybackItemDict[args.NewItem];
                    await UpsertSongWithPreloadInPlaybackList(newSong);
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

        public void SetLoop(bool loop)
        {
            mediaPlaybackList.AutoRepeatEnabled = loop;
        }

        public async Task SetSongs(ICollection<Song> songs, bool keepList)
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

                if (keepList && mediaPlaybackList.Items.Count > 0)
                {
                    // insert next so to "make" time for updating the rest of the list
                    int currentSongIndex = songsIndexes[mediaPlaybackItemDict[mediaPlaybackList.CurrentItem]];
                    Song nextSong = songs.ElementAtCycle(currentSongIndex + 1);
                    MediaPlaybackItem nextItem = mediaPlaybackList.Items.FirstOrDefault(item => mediaPlaybackItemDict[item] == nextSong);
                    int nextIndexList = nextSong == songs.First() ? 0 : (int)mediaPlaybackList.CurrentItemIndex;
                    if (nextItem != null)
                    {
                        mediaPlaybackList.Items.Remove(nextItem);
                        mediaPlaybackList.Items.Insert(nextIndexList, nextItem);
                    }
                    else nextItem = await InsertSongIntoMediaPlaybackList(nextSong, nextIndexList);

                    // remove everything excpet current and next song
                    for (int i = mediaPlaybackList.Items.Count - 1; i >= 0; i--)
                    {
                        if (i != mediaPlaybackList.CurrentItemIndex&& mediaPlaybackList.Items[i] != nextItem)
                        {
                            mediaPlaybackList.Items.RemoveAt(i);
                        }
                    }

                    await UpsertSongWithPreloadInPlaybackList(mediaPlaybackItemDict[mediaPlaybackList.CurrentItem]);
                }
                else
                {
                    mediaPlaybackList.Items.Clear();
                    mediaPlaybackItemDict.Clear();
                }
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
                    ExecutePlayState();
                    return;
                }

                setRequest = request;
                MediaPlaybackItem item = await UpsertSongWithPreloadInPlaybackList(request.Song);
                if (item != mediaPlaybackList.CurrentItem)
                {
                    int index = mediaPlaybackList.Items.IndexOf(item);
                    mediaPlaybackList.MoveTo((uint)index);
                }

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
                MediaFailed?.Invoke(this, new MediaFailedEventArgs(request.Song, e));
            }
            finally
            {
                sem.Release();
            }
        }

        private async Task<MediaPlaybackItem> UpsertSongWithPreloadInPlaybackList(Song upsertSong)
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
            foreach (Song insertSong in GetSongsToUpsert(upsertSong))
            {
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
                            // check for start time
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
                    MediaPlaybackItem mediaPlaybackItem = await InsertSongIntoMediaPlaybackList(insertSong, listInsertIndex);
                    if (insertSong == upsertSong) result = mediaPlaybackItem;
                }
            }

            return result;
        }

        private async Task<MediaPlaybackItem> InsertSongIntoMediaPlaybackList(Song song, int index)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(song.FullPath);
            MediaSource mediaSource = MediaSource.CreateFromStorageFile(file);
            MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            mediaPlaybackItem.AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music;
            mediaPlaybackItemDict[mediaPlaybackItem] = song;
            mediaPlaybackList.Items.Insert(index, mediaPlaybackItem);

            return mediaPlaybackItem;
        }

        private IEnumerable<Song> GetSongsToUpsert(Song mainSong)
        {
            yield return mainSong;

            int mainSongIndex = songsIndexes[mainSong];
            for (int i = mainSongIndex - minPreLoadMediaSources; i < mainSongIndex + minPreLoadMediaSources; i++)
            {
                if (i != mainSongIndex)
                {
                    yield return songs.ElementAtCycle(i);
                }
            }
        }

        public async Task Stop()
        {
            request = null;
            await sem.WaitAsync();

            try
            {
                mediaPlaybackList.Items.Clear();
                mediaPlaybackItemDict.Clear();
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

            songs = new Song[0];
            songsIndexes.Clear();
            mediaPlaybackItemDict.Clear();
            mediaPlaybackList.Items.Clear();

            player.Dispose();
        }
    }
}
