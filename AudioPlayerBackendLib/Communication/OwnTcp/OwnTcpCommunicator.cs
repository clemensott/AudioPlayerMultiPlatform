using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public abstract class OwnTcpCommunicator : BaseCommunicator
    {
        public const string AnwserCmd = "-ans", ReturnCmd = "-rtn", SyncCmd = "-sync", PingCmd = "-ping", CloseCmd = "-close";

        protected readonly IAudioCreateService audioCreateService;

        protected OwnTcpCommunicator()
        {
            audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
        }

        protected override async void OnFileMediaSourceRootsChanged(object sender, ValueChangedEventArgs<FileMediaSourceRoot[]> e)
        {
            await PublishFileMediaSourceRoots();
        }

        protected Task PublishFileMediaSourceRoots()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.FileMediaSourceRoots);
            return SendAsync(nameof(Service.FileMediaSourceRoots), data, false);
        }

        protected override async void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            await PublishCurrentPlaylist();
        }

        protected Task PublishCurrentPlaylist()
        {
            ByteQueue data = new ByteQueue();
            string playlistType = GetPlaylistType(Service.CurrentPlaylist);
            data.Enqueue(playlistType);

            switch (playlistType)
            {
                case nameof(ISourcePlaylistBase):
                    data.Enqueue((ISourcePlaylistBase)Service.CurrentPlaylist);
                    break;

                case nameof(IPlaylistBase):
                    data.Enqueue(Service.CurrentPlaylist);
                    break;
            }

            return SendAsync(nameof(Service.CurrentPlaylist), data, false);
        }

        protected override async void OnServiceSourcePlaylistsChanged(object sender, ValueChangedEventArgs<ISourcePlaylistBase[]> e)
        {
            List<ISourcePlaylistBase> added = new List<ISourcePlaylistBase>();

            foreach (ISourcePlaylistBase playlist in e.OldValue.Except(e.NewValue))
            {
                Unsubscribe(playlist);
                if (playlists.ContainsKey(playlist.ID)) playlists.Remove(playlist.ID);
            }

            foreach (ISourcePlaylistBase playlist in e.NewValue.Except(e.OldValue))
            {
                Subscribe(playlist);

                added.Add(playlist);
                playlists[playlist.ID] = playlist;
            }

            foreach (ISourcePlaylistBase playlist in e.NewValue)
            {
                playlists[playlist.ID] = playlist;
            }

            ByteQueue queue = new ByteQueue();
            queue.Enqueue(added);
            queue.Enqueue(e.NewValue.Select(p => p.ID));

            await SendAsync(nameof(Service.SourcePlaylists), queue, false);
        }

        protected override async void OnServicePlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            List<IPlaylistBase> added = new List<IPlaylistBase>();

            foreach (IPlaylistBase playlist in e.OldValue.Except(e.NewValue))
            {
                Unsubscribe(playlist);
                if (playlists.ContainsKey(playlist.ID)) playlists.Remove(playlist.ID);
            }

            foreach (IPlaylistBase playlist in e.NewValue.Except(e.OldValue))
            {
                Subscribe(playlist);

                added.Add(playlist);
                playlists[playlist.ID] = playlist;
            }

            foreach (IPlaylistBase playlist in e.NewValue)
            {
                playlists[playlist.ID] = playlist;
            }

            ByteQueue queue = new ByteQueue();
            queue.Enqueue(added);
            queue.Enqueue(e.NewValue.Select(p => p.ID));

            await SendAsync(nameof(Service.Playlists), queue, false);
        }

        protected override async void OnServicePlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            await PublishPlayState();
        }

        protected Task PublishPlayState()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)Service.PlayState);

            return SendAsync(nameof(Service.PlayState), data, false);
        }

        protected override async void OnServiceVolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            await PublishVolume();
        }

        protected Task PublishVolume()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.Volume);

            return SendAsync(nameof(Service.Volume), data, true);
        }

        protected override async void OnPlaylistFileMediaSourcesChanged(object sender, ValueChangedEventArgs<FileMediaSource[]> e)
        {
            await PublishMediaSources((ISourcePlaylistBase)sender);
        }

        protected Task PublishMediaSources(ISourcePlaylistBase source)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(source.FileMediaSources);

            return SendAsync(source, nameof(source.FileMediaSources), data, false);
        }

        protected override async void OnPlaylistCurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            await PublishCurrentSong((IPlaylistBase)sender);
        }

        protected Task PublishCurrentSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.CurrentSong);

            return SendAsync(playlist, nameof(playlist.CurrentSong), data, false);
        }

        protected override async void OnPlaylistDurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishDuration((IPlaylistBase)sender);
        }

        protected Task PublishDuration(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Duration);

            return SendAsync(playlist, nameof(playlist.Duration), data, false);
        }

        protected override async void OnPlaylistShuffleChanged(object sender, ValueChangedEventArgs<OrderType> e)
        {
            await PublishShuffle((IPlaylistBase)sender);
        }

        protected Task PublishShuffle(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Shuffle);

            return SendAsync(playlist, nameof(playlist.Shuffle), data, false);
        }

        protected override async void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
            await PublishLoop((IPlaylistBase)sender);
        }

        protected Task PublishLoop(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Loop);

            return SendAsync(playlist, nameof(playlist.Loop), data, false);
        }

        protected override async void OnPlaylistNameChanged(object sender, ValueChangedEventArgs<string> e)
        {
            await PublishName((IPlaylistBase)sender);
        }

        private Task PublishName(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Name);

            return SendAsync(playlist, nameof(playlist.Name), data, false);
        }

        protected override async void OnPlaylistPositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishPosition((IPlaylistBase)sender);
        }

        protected Task PublishPosition(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Position);

            return SendAsync(playlist, nameof(playlist.Position), data, false);
        }

        protected override async void OnPlaylistWannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            await PublishWannaSong((IPlaylistBase)sender);
        }

        private Task PublishWannaSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.WannaSong);

            return SendAsync(playlist, nameof(playlist.WannaSong), data, false);
        }

        protected override async void OnPlaylistSongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            await PublishSongs((IPlaylistBase)sender);
        }

        protected Task PublishSongs(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Songs);

            return SendAsync(playlist, nameof(playlist.Songs), data, false);
        }

        private Task SendAsync(IPlaylistBase playlist, string topic, byte[] payload, bool fireAndForget)
        {
            return SendAsync(playlist.ID + "." + topic, payload, fireAndForget);
        }

        protected abstract Task SendAsync(string topic, byte[] payload, bool fireAndForget);

        protected static IEnumerable<byte> GetBytes(OwnTcpMessage message)
        {
            byte[] idBytes = BitConverter.GetBytes(message.ID);
            byte[] fireAndForgetBytes = BitConverter.GetBytes(message.IsFireAndForget);
            byte[] topicBytes = Encoding.UTF8.GetBytes(message.Topic);
            byte[] topicLengthBytes = BitConverter.GetBytes(topicBytes.Length);
            byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload?.Length ?? -1);

            return idBytes.Concat(fireAndForgetBytes)
                .Concat(topicLengthBytes)
                .Concat(topicBytes)
                .Concat(payloadLengthBytes)
                .Concat(message.Payload ?? new byte[0]);
        }

        protected bool HandlerMessage(OwnTcpMessage message)
        {
            string topic;
            Guid id;

            return ContainsPlaylist(message.Topic, out topic, out id) ?
                HandlePlaylistMessage(id, topic, message.Payload) :
                HandleServiceMessage(topic, message.Payload);
        }

        private static bool ContainsPlaylist(string rawTopic, out string topic, out Guid id)
        {
            if (!rawTopic.Contains('.'))
            {
                topic = rawTopic;
                id = Guid.Empty;
                return false;
            }

            id = Guid.Parse(rawTopic.Remove(36));
            topic = rawTopic.Substring(37);

            return true;
        }

        private bool HandleServiceMessage(string topic, ByteQueue data)
        {
            switch (topic)
            {
                case nameof(Service.SourcePlaylists):
                    HandleSourcePlaylistsTopic(data);
                    break;

                case nameof(Service.Playlists):
                    HandlePlaylistsTopic(data);
                    break;

                case nameof(Service.AudioData):
                    Service.AudioData = data;
                    break;

                case nameof(Service.CurrentPlaylist):
                    HandleCurrentPlaylistTopic(data);
                    break;

                case nameof(Service.PlayState):
                    Service.PlayState = (PlaybackState)data.DequeueInt();
                    break;

                case nameof(Service.Volume):
                    Service.Volume = data.DequeueFloat();
                    break;

                case nameof(Service.IsSearchShuffle):
                    Service.IsSearchShuffle = data.DequeueBool();
                    break;

                case nameof(Service.SearchKey):
                    Service.SearchKey = data.DequeueString();
                    break;

                case cmdString:
                    IAudioService service;
                    string cmd = Encoding.UTF8.GetString(data);

                    switch (cmd.ToLower())
                    {
                        case "play":
                            Service.PlayState = PlaybackState.Playing;
                            break;

                        case "pause":
                            Service.PlayState = PlaybackState.Paused;
                            break;

                        case "toggle":
                            Service.PlayState = Service.PlayState != PlaybackState.Playing
                                ? PlaybackState.Playing
                                : PlaybackState.Paused;
                            break;

                        case "next":
                            service = Service as IAudioService;
                            //service?.SetNextSong();
                            break;

                        case "previous":
                            service = Service as IAudioService;
                            //service?.SetPreviousSong();
                            break;

                        default:
                            return false;
                    }
                    break;

                default:
                    return false;
            }

            return true;
        }

        private void HandleSourcePlaylistsTopic(ByteQueue data)
        {
            Dictionary<Guid, ISourcePlaylistBase> addPlaylists = data
                    .DequeueSourcePlaylists(audioCreateService.CreateSourcePlaylist)
                    .ToDictionary(p => p.ID);
            Guid[] order = data.DequeueGuids();
            ISourcePlaylistBase[] newPlaylists = new ISourcePlaylistBase[order.Length];

            for (int i = 0; i < order.Length; i++)
            {
                ISourcePlaylistBase playlist;
                Guid id = order[i];
                if (addPlaylists.TryGetValue(id, out playlist))
                {
                    playlists[id] = newPlaylists[i] = playlist;
                }
                else newPlaylists[i] = (ISourcePlaylistBase)playlists[id];
            }

            Service.SourcePlaylists = newPlaylists;
        }

        private void HandlePlaylistsTopic(ByteQueue data)
        {
            Dictionary<Guid, IPlaylistBase> addPlaylists = data
                    .DequeuePlaylists(audioCreateService.CreatePlaylist)
                    .ToDictionary(p => p.ID);
            Guid[] order = data.DequeueGuids();
            IPlaylistBase[] newPlaylists = new IPlaylistBase[order.Length];

            for (int i = 0; i < order.Length; i++)
            {
                IPlaylistBase playlist;
                Guid id = order[i];
                if (addPlaylists.TryGetValue(id, out playlist))
                {
                    newPlaylists[i] = playlists[id] = playlist;
                }
                else newPlaylists[i] = playlists[id];
            }

            Service.Playlists = newPlaylists;
        }

        private void HandleCurrentPlaylistTopic(ByteQueue data)
        {
            IPlaylistBase currentPlaylist, existingPlaylist;
            switch (data.DequeueString())
            {
                case nameof(ISourcePlaylistBase):
                    currentPlaylist = data.DequeueSourcePlaylist(audioCreateService.CreateSourcePlaylist);
                    break;

                case nameof(IPlaylistBase):
                    currentPlaylist = data.DequeuePlaylist(audioCreateService.CreatePlaylist);
                    break;

                default:
                    currentPlaylist = null;
                    break;
            }

            if (currentPlaylist == null) Service.CurrentPlaylist = null;
            else if (playlists.TryGetValue(currentPlaylist.ID, out existingPlaylist))
            {
                Service.CurrentPlaylist = existingPlaylist;
            }
            else
            {
                playlists.Add(currentPlaylist.ID, currentPlaylist);
                Service.CurrentPlaylist = currentPlaylist;
            }
        }

        private bool HandlePlaylistMessage(Guid id, string topic, ByteQueue data)
        {
            IPlaylistBase playlist = playlists[id];
            ISourcePlaylistBase source = playlist as ISourcePlaylistBase;

            switch (topic)
            {
                case nameof(playlist.CurrentSong):
                    playlist.CurrentSong = data.DequeueNullableSong();
                    break;

                case nameof(playlist.Duration):
                    playlist.Duration = data.DequeueTimeSpan();
                    break;

                case nameof(playlist.Shuffle):
                    playlist.Shuffle = (OrderType)data.DequeueInt();
                    break;

                case nameof(playlist.Loop):
                    playlist.Loop = (LoopType)data.DequeueInt();
                    break;

                case nameof(playlist.Name):
                    playlist.Name = data.DequeueString();
                    break;

                case nameof(playlist.Position):
                    playlist.Position = data.DequeueTimeSpan();
                    break;

                case nameof(playlist.WannaSong):
                    playlist.WannaSong = data.DequeueNullableRequestSong();
                    break;

                case nameof(playlist.Songs):
                    playlist.Songs = data.DequeueSongs();
                    break;

                case nameof(source.FileMediaSources):
                    source.FileMediaSources = data.DequeueFileMediaSources();
                    break;

                default:
                    return false;
            }

            return true;
        }
    }
}
