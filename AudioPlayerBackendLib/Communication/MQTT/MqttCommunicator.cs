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

        protected MqttCommunicator(INotifyPropertyChangedHelper helper = null) : base(helper)
        {
        }

        protected async void InitPlaylists()
        {
            playlists.Clear();

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
            await PublishAsync(nameof(Service.AudioData), Service.AudioData, MqttQualityOfServiceLevel.AtMostOnce);
        }

        protected override async void OnServiceAudioFormatChanged(object sender, ValueChangedEventArgs<WaveFormat> e)
        {
            await PublishFormat();
        }

        protected async Task PublishFormat()
        {
            ByteQueue data = new ByteQueue();
            if (Service.AudioFormat != null) data.Enqueue(Service.AudioFormat);

            await PublishAsync(nameof(Service.AudioFormat), data);
        }

        protected override async void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            await PublishCurrentPlaylist();
        }

        protected async Task PublishCurrentPlaylist()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.CurrentPlaylist.ID);

            await Task.WhenAll(PublishAsync(nameof(Service.CurrentPlaylist), data), AddPlaylist(Service.CurrentPlaylist));
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
            tasks.Add(PublishAsync(nameof(Service.Playlists), data));
            tasks.AddRange(Service.Playlists.Select(AddPlaylist));

            await Task.WhenAll(tasks);
        }

        protected async Task PublishPlaylist(IPlaylistBase playlist)
        {
            await Task.WhenAll(PublishIsAllShuffle(playlist), PublishLoop(playlist), PublishPosition(playlist),
                PublishDuration(playlist), PublishWannaSong(playlist), PublishSongs(playlist), PublishCurrentSong(playlist));
        }

        protected override async void OnServicePlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            await PublishPlayState();
        }

        protected async Task PublishPlayState()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)Service.PlayState);

            await PublishAsync(nameof(Service.PlayState), data);
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

                await PublishAsync(nameof(Service.Volume), data, MqttQualityOfServiceLevel.AtMostOnce);
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

            await PublishAsync(source, nameof(source.FileMediaSources), data);
        }

        protected override async void OnPlaylistIsSearchShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            await PublishIsSearchShuffle((ISourcePlaylistBase)sender);
        }

        protected async Task PublishIsSearchShuffle(ISourcePlaylistBase playlist)
        {
            return;
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.IsSearchShuffle);

            await PublishAsync(playlist, nameof(playlist.IsSearchShuffle), data);
        }

        protected override async void OnPlaylistSearchKeyChanged(object sender, ValueChangedEventArgs<string> e)
        {
            await PublishSearchKey((ISourcePlaylistBase)sender);
        }

        protected async Task PublishSearchKey(ISourcePlaylistBase playlist)
        {
            return;
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.SearchKey);

            await PublishAsync(playlist, nameof(playlist.SearchKey), data);
        }

        protected override async void OnPlaylistCurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            await PublishCurrentSong((IPlaylistBase)sender);
        }

        protected async Task PublishCurrentSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.CurrentSong);

            await PublishAsync(playlist, nameof(playlist.CurrentSong), data);
        }

        protected override async void OnPlaylistDurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishDuration((IPlaylistBase)sender);
        }

        protected async Task PublishDuration(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Duration);

            await PublishAsync(playlist, nameof(playlist.Duration), data);
        }

        protected override async void OnPlaylistIsAllShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            await PublishIsAllShuffle((IPlaylistBase)sender);
        }

        protected async Task PublishIsAllShuffle(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.IsAllShuffle);

            await PublishAsync(playlist, nameof(playlist.IsAllShuffle), data);
        }

        protected override async void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
            await PublishLoop((IPlaylistBase)sender);
        }

        protected async Task PublishLoop(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Loop);

            await PublishAsync(playlist, nameof(playlist.Loop), data);
        }

        protected override async void OnPlaylistPositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishPosition((IPlaylistBase)sender);
        }

        protected async Task PublishPosition(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Position);

            await PublishAsync(playlist, nameof(playlist.Position), data);
        }

        protected override async void OnPlaylistWannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            await PublishWannaSong((IPlaylistBase)sender);
        }

        private async Task PublishWannaSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.WannaSong);

            await PublishAsync(playlist, nameof(playlist.WannaSong), data, MqttQualityOfServiceLevel.AtMostOnce, false);
        }

        protected override async void OnPlaylistSongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            await PublishSongs((IPlaylistBase)sender);
        }

        protected async Task PublishSongs(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Songs);

            await PublishAsync(playlist, nameof(playlist.Songs), data);
        }

        public override async Task SendCommand(string cmd)
        {
            byte[] payload = Encoding.UTF8.GetBytes(cmd);

            await PublishAsync(cmdString, payload, MqttQualityOfServiceLevel.AtMostOnce, false);
        }

        private async Task PublishAsync(IPlaylistBase playlist, string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            await PublishAsync(playlist.ID + "." + topic, payload, qos, retain);
        }

        private async Task PublishAsync(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
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
            string topic;
            Guid id;

            if (ContainsPlaylist(rawTopic, out topic, out id))
            {
                bool handled = await HandlePlaylistMessage(id, topic, payload);

                InitList<string> initPlaylistList;
                if (initPlaylistLists.TryGetValue(id, out initPlaylistList)) initPlaylistList?.Remove(rawTopic);

                return handled;
            }

            return HandleServiceMessage(topic, payload);
        }

        private bool HandleServiceMessage(string topic, ByteQueue data)
        {
            switch (topic)
            {
                case nameof(Service.Playlists):
                    HandlePlaylistsTopic(data);
                    break;

                case nameof(Service.AudioData):
                    Service.AudioData = data;
                    break;

                case nameof(Service.CurrentPlaylist):
                    HandleCurrentPlaylistTopic(data);
                    break;

                case nameof(Service.AudioFormat):
                    Service.AudioFormat = data.DequeueWaveFormat();
                    break;

                case nameof(Service.PlayState):
                    Service.PlayState = (PlaybackState)data.DequeueInt();
                    break;

                case nameof(Service.Volume):
                    Service.Volume = data.DequeueFloat();
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

        private async void HandlePlaylistsTopic(ByteQueue data)
        {
            Task<IPlaylistBase>[] tasks = data.DequeueGuids().Select(guid => GetInitPlaylist(guid)).ToArray();

            await Task.WhenAll(tasks);

            Service.Playlists = tasks.Select(t => t.Result).ToArray();
        }

        private async void HandleCurrentPlaylistTopic(ByteQueue data)
        {
            Service.CurrentPlaylist = await GetInitPlaylist(data.DequeueGuid());
        }

        private async Task<bool> HandlePlaylistMessage(Guid id, string topic, ByteQueue data)
        {
            IPlaylistBase playlist = await GetPlaylist(id);
            ISourcePlaylistBase source = playlist as ISourcePlaylistBase;

            switch (topic)
            {
                case nameof(playlist.CurrentSong):
                    playlist.CurrentSong = data.DequeueNullableSong();
                    break;

                case nameof(playlist.Duration):
                    playlist.Duration = data.DequeueTimeSpan();
                    break;

                case nameof(playlist.IsAllShuffle):
                    playlist.IsAllShuffle = data.DequeueBool();
                    break;

                case nameof(playlist.Loop):
                    playlist.Loop = (LoopType)data.DequeueInt();
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

                case nameof(source.IsSearchShuffle):
                    source.IsSearchShuffle = data.DequeueBool();
                    break;

                case nameof(source.SearchKey):
                    source.SearchKey = data.DequeueString();
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

        private async Task<IPlaylistBase> GetPlaylist(Guid id)
        {
            if (id == Guid.Empty) return Service.SourcePlaylist;

            IPlaylistBase playlist;
            if (!playlists.TryGetValue(id, out playlist))
            {
                playlist = new Playlist(id, helper);

                await InitPlaylist(playlist, false);
            }

            return playlist;
        }

        private async Task<IPlaylistBase> GetInitPlaylist(Guid id)
        {
            InitList<string> initPlaylistList;
            IPlaylistBase playlist;

            if (id == Guid.Empty) return Service.SourcePlaylist;
            if (initPlaylistLists.TryGetValue(id, out initPlaylistList)) await initPlaylistList.Task;
            if (playlists.TryGetValue(id, out playlist)) return playlist;

            playlist = new Playlist(id);

            await InitPlaylist(playlist, true);

            return playlist;
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
            string id = playlist.ID + ".";

            yield return id + nameof(playlist.CurrentSong);
            yield return id + nameof(playlist.Songs);
            yield return id + nameof(playlist.Duration);
            yield return id + nameof(playlist.IsAllShuffle);
            yield return id + nameof(playlist.Loop);
            yield return id + nameof(playlist.Position);
            yield return id + nameof(playlist.WannaSong);
        }

        private async Task AddPlaylist(IPlaylistBase playlist)
        {
            await AddPlaylist(playlist, true);
        }

        private async Task AddPlaylist(IPlaylistBase playlist, bool publish)
        {
            if (playlist.ID == Guid.Empty || playlists.ContainsKey(playlist.ID)) return;

            playlists.Add(playlist.ID, playlist);

            await SubscribeAsync(playlist);
            if (publish) await PublishPlaylist(playlist);
        }
    }
}
