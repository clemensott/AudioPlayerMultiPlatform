using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using MQTTnet;
using MQTTnet.Protocol;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Communication.Base;

namespace AudioPlayerBackend.Communication.MQTT
{
    public abstract class MqttCommunicator : BaseCommunicator
    {
        private readonly Dictionary<Guid, InitList<string>> initPlaylistLists = new Dictionary<Guid, InitList<string>>();

        protected readonly IAudioCreateService audioCreateService;

        protected MqttCommunicator()
        {
            audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
        }

        protected async void InitPlaylists()
        {
            playlists.Clear();

            foreach (IPlaylistBase playlist in (Service?.SourcePlaylists).ToNotNull())
            {
                await AddPlaylist(playlist);
            }

            foreach (IPlaylistBase playlist in (Service?.Playlists).ToNotNull())
            {
                await AddPlaylist(playlist);
            }
        }

        protected abstract Task SubscribeAsync(IPlaylistBase playlist);

        protected override async void OnServiceAudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
            await PublishAudioData();
        }

        protected async Task PublishAudioData()
        {
            await PublishServiceAsync(nameof(Service.AudioData), Service.AudioData, MqttQualityOfServiceLevel.AtMostOnce);
        }

        protected override async void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            await PublishCurrentPlaylist();
        }

        protected async Task PublishCurrentPlaylist()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(GetPlaylistType(Service.CurrentPlaylist));
            data.Enqueue(Service.CurrentPlaylist?.ID);

            await Task.WhenAll(PublishServiceAsync(nameof(Service.CurrentPlaylist), data), AddPlaylist(Service.CurrentPlaylist));
        }

        protected override async void OnServiceSourcePlaylistsChanged(object sender, ValueChangedEventArgs<ISourcePlaylistBase[]> e)
        {
            foreach (IPlaylistBase playlist in e.NewValue.Except(e.OldValue))
            {
                Subscribe(playlist);
            }

            foreach (IPlaylistBase playlist in e.OldValue.Except(e.NewValue))
            {
                Unsubscribe(playlist);
            }

            await PublishSourcePlaylists();
        }

        protected async Task PublishSourcePlaylists()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.SourcePlaylists.Select(p => p.ID));

            if (IsTopicLocked(nameof(Service.SourcePlaylists), data)) return;

            List<Task> tasks = new List<Task>();
            tasks.Add(PublishServiceAsync(nameof(Service.SourcePlaylists), data));
            tasks.AddRange(Service.SourcePlaylists.Select(AddPlaylist));

            await Task.WhenAll(tasks);
        }

        protected override async void OnServicePlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            foreach (IPlaylistBase playlist in e.NewValue.Except(e.OldValue))
            {
                Subscribe(playlist);
            }

            foreach (IPlaylistBase playlist in e.OldValue.Except(e.NewValue))
            {
                Unsubscribe(playlist);
            }

            await PublishPlaylists();
        }

        protected async Task PublishPlaylists()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.Playlists.Select(p => p.ID));

            if (IsTopicLocked(nameof(Service.Playlists), data)) return;

            List<Task> tasks = new List<Task>();
            tasks.Add(PublishServiceAsync(nameof(Service.Playlists), data));
            tasks.AddRange(Service.Playlists.Select(AddPlaylist));

            await Task.WhenAll(tasks);
        }

        protected Task PublishPlaylist(IPlaylistBase playlist)
        {
            return Task.WhenAll(Publish());

            IEnumerable<Task> Publish()
            {
                yield return PublishShuffle(playlist);
                yield return PublishLoop(playlist);
                yield return PublishName(playlist);
                yield return PublishPosition(playlist);
                yield return PublishDuration(playlist);
                yield return PublishWannaSong(playlist);
                yield return PublishSongs(playlist);
                yield return PublishCurrentSong(playlist);

                if (playlist is ISourcePlaylistBase source) yield return PublishMediaSources(source);
            }
        }

        protected override async void OnServicePlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            await PublishPlayState();
        }

        protected async Task PublishPlayState()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)Service.PlayState);

            await PublishServiceAsync(nameof(Service.PlayState), data);
        }

        protected override async void OnServiceVolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            await PublishVolume();
        }

        protected async Task PublishVolume()
        {
            try
            {
                ByteQueue data = new ByteQueue();
                data.Enqueue(Service.Volume);

                await PublishServiceAsync(nameof(Service.Volume), data, MqttQualityOfServiceLevel.AtMostOnce);
            }
            catch { }
        }

        protected override async void OnPlaylistFileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
        {
            await PublishMediaSources((ISourcePlaylistBase)sender);
        }

        protected async Task PublishMediaSources(ISourcePlaylistBase source)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(source.FileMediaSources);

            await PublishPlaylistAsync(source, nameof(source.FileMediaSources), data);
        }

        protected override async void OnPlaylistIsSearchShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            await PublishIsSearchShuffle();
        }

        protected async Task PublishIsSearchShuffle()
        {
            return;
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.IsSearchShuffle);

            await PublishServiceAsync(nameof(Service.IsSearchShuffle), data);
        }

        protected override async void OnPlaylistSearchKeyChanged(object sender, ValueChangedEventArgs<string> e)
        {
            await PublishSearchKey();
        }

        protected async Task PublishSearchKey()
        {
            return;
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.SearchKey);

            await PublishServiceAsync(nameof(Service.SearchKey), data);
        }

        protected override async void OnPlaylistCurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            await PublishCurrentSong((IPlaylistBase)sender);
        }

        protected Task PublishCurrentSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.CurrentSong);

            return PublishPlaylistAsync(playlist, nameof(playlist.CurrentSong), data);
        }

        protected override async void OnPlaylistDurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishDuration((IPlaylistBase)sender);
        }

        protected Task PublishDuration(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Duration);

            return PublishPlaylistAsync(playlist, nameof(playlist.Duration), data);
        }

        protected override async void OnPlaylistShuffleChanged(object sender, ValueChangedEventArgs<OrderType> e)
        {
            await PublishShuffle((IPlaylistBase)sender);
        }

        protected Task PublishShuffle(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Shuffle);

            return PublishPlaylistAsync(playlist, nameof(playlist.Shuffle), data);
        }

        protected override async void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
            await PublishLoop((IPlaylistBase)sender);
        }

        protected Task PublishLoop(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Loop);

            return PublishPlaylistAsync(playlist, nameof(playlist.Loop), data);
        }

        protected override async void OnPlaylistNameChanged(object sender, ValueChangedEventArgs<string> e)
        {
            await PublishName((IPlaylistBase)sender);
        }

        private Task PublishName(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Name);

            return PublishPlaylistAsync(playlist, nameof(playlist.Name), data);
        }

        protected override async void OnPlaylistPositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishPosition((IPlaylistBase)sender);
        }

        protected Task PublishPosition(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Position);

            return PublishPlaylistAsync(playlist, nameof(playlist.Position), data);
        }

        protected override async void OnPlaylistWannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            await PublishWannaSong((IPlaylistBase)sender);
        }

        private Task PublishWannaSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.WannaSong);

            return PublishPlaylistAsync(playlist, nameof(playlist.WannaSong), data, MqttQualityOfServiceLevel.AtMostOnce, false);
        }

        protected override async void OnPlaylistSongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            await PublishSongs((IPlaylistBase)sender);
        }

        protected Task PublishSongs(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Songs);

            return PublishPlaylistAsync(playlist, nameof(playlist.Songs), data);
        }

        public override Task SendCommand(string cmd)
        {
            byte[] payload = Encoding.UTF8.GetBytes(cmd);

            return PublishServiceAsync(cmdString, payload, MqttQualityOfServiceLevel.AtMostOnce, false);
        }

        private Task PublishPlaylistAsync(IPlaylistBase playlist, string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            return PublishAsync($"{GetPlaylistType(playlist)}.{playlist.ID}.{topic}", payload, qos, retain);
        }

        private Task PublishServiceAsync(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            return PublishAsync($"{nameof(IAudioServiceBase)}.{topic}", payload, qos, retain);
        }

        private async Task PublishAsync(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            //System.Diagnostics.Debug.WriteLine("Topic: " + topic);
            await PublishAsync(new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            });
        }

        protected abstract Task PublishAsync(MqttApplicationMessage message);

        protected async Task PublishDebug(string rawTopic, Exception e)
        {
            string text = "rawTopic: " + rawTopic + "\r\n" + e;

            await PublishDebug(text);
        }

        protected async Task PublishDebug(Exception e)
        {
            await PublishDebug(e.ToString());
        }

        protected async Task PublishDebug(string text)
        {
            try
            {
                MqttApplicationMessage message = new MqttApplicationMessage()
                {
                    Topic = "Debug",
                    Payload = Encoding.UTF8.GetBytes(text),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = true
                };

                await PublishAsync(message);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        protected async Task<bool> HandleMessage(string rawTopic, byte[] payload)
        {
            System.Diagnostics.Debug.WriteLine("Handle message: " + rawTopic);
            string[] parts = rawTopic.Split('.');

            if (parts.Length == 1) return false;
            if (parts[0] == nameof(IAudioServiceBase)) return HandleServiceMessage(parts[1], payload);

            Guid id = Guid.Parse(parts[1]);
            bool handled = await HandlePlaylistMessage(id, parts[2], payload, parts[0]);

            InitList<string> initPlaylistList;
            if (initPlaylistLists.TryGetValue(id, out initPlaylistList)) initPlaylistList?.Remove(rawTopic);

            return handled;

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
                            if (service != null) service.SetNextSong();
                            break;

                        case "previous":
                            service = Service as IAudioService;
                            if (service != null) service.SetPreviousSong();
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

        private async void HandleSourcePlaylistsTopic(ByteQueue data)
        {
            Task<IPlaylistBase>[] tasks = data.DequeueGuids().Select(guid => GetInitPlaylist(guid, nameof(ISourcePlaylistBase))).ToArray();

            Service.SourcePlaylists = (await Task.WhenAll(tasks)).Cast<ISourcePlaylistBase>().ToArray();
        }

        private async void HandlePlaylistsTopic(ByteQueue data)
        {
            Task<IPlaylistBase>[] tasks = data.DequeueGuids().Select(guid => GetInitPlaylist(guid, nameof(IPlaylistBase))).ToArray();

            Service.Playlists = (await Task.WhenAll(tasks)).ToArray();
        }

        private async void HandleCurrentPlaylistTopic(ByteQueue data)
        {
            string playlistType = data.DequeueString();
            Guid? id = data.DequeueNullableGuid();
            Service.CurrentPlaylist = await GetInitPlaylist(id, playlistType);
        }

        private async Task<bool> HandlePlaylistMessage(Guid id, string topic, ByteQueue data, string playlistType)
        {
            IPlaylistBase playlist = await GetPlaylist(id, playlistType);
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
                    return false;

                case nameof(playlist.Songs):
                    playlist.Songs = data.DequeueSongs();
                    break;

                case nameof(source.FileMediaSources):
                    source.FileMediaSources = data.DequeueStrings();
                    break;

                default:
                    return false;
            }

            return true;
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

        private async Task<IPlaylistBase> GetPlaylist(Guid id, string playlistType)
        {
            IPlaylistBase playlist;
            if (!playlists.TryGetValue(id, out playlist))
            {
                playlist = CreatePlaylist(playlistType, id);

                await InitPlaylist(playlist, false);
            }

            return playlist;
        }

        private async Task<IPlaylistBase> GetInitPlaylist(Guid? id, string playlistType)
        {
            InitList<string> initPlaylistList;
            IPlaylistBase playlist;

            if (id == null) return null;
            if (initPlaylistLists.TryGetValue(id.Value, out initPlaylistList)) await initPlaylistList.Task;
            if (playlists.TryGetValue(id.Value, out playlist)) return playlist;

            playlist = CreatePlaylist(playlistType, id.Value);

            await InitPlaylist(playlist, true);

            return playlist;
        }

        private IPlaylistBase CreatePlaylist(string playlistType, Guid id)
        {
            switch (playlistType)
            {
                case nameof(ISourcePlaylistBase):
                    return audioCreateService.CreateSourcePlaylist(id);

                case nameof(IPlaylistBase):
                    return audioCreateService.CreatePlaylist(id);
            }

            throw new ArgumentException($"Type is not supported: {playlistType}", nameof(playlistType));
        }

        private async Task InitPlaylist(IPlaylistBase playlist, bool wait)
        {
            InitList<string> initPlaylistList = new InitList<string>(GetTopics(playlist));
            initPlaylistLists.Add(playlist.ID, initPlaylistList);

            await AddPlaylist(playlist, false);

            Task waitTask = WaitInitiation();
            if (wait) await waitTask;

            async Task WaitInitiation()
            {
                await initPlaylistList.Task;

                initPlaylistLists.Remove(playlist.ID);
            }
        }

        protected static IEnumerable<string> GetTopics(IPlaylistBase playlist)
        {
            string prefix = $"{GetPlaylistType(playlist)}.{ playlist.ID}.";

            yield return prefix + nameof(playlist.CurrentSong);
            yield return prefix + nameof(playlist.Songs);
            yield return prefix + nameof(playlist.Duration);
            yield return prefix + nameof(playlist.Shuffle);
            yield return prefix + nameof(playlist.Loop);
            yield return prefix + nameof(playlist.Name);
            yield return prefix + nameof(playlist.Position);

            if (playlist is ISourcePlaylistBase) yield return prefix + nameof(ISourcePlaylistBase.FileMediaSources);
        }

        private async Task AddPlaylist(IPlaylistBase playlist)
        {
            await AddPlaylist(playlist, true);
        }

        private async Task AddPlaylist(IPlaylistBase playlist, bool publish)
        {
            if (playlist == null || playlists.ContainsKey(playlist.ID)) return;

            playlists.Add(playlist.ID, playlist);

            await SubscribeAsync(playlist);
            if (publish) await PublishPlaylist(playlist);
        }
    }
}
