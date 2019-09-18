using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using MQTTnet;
using MQTTnet.Protocol;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.MQTT
{
    public abstract class MqttCommunicator : ICommunicator, INotifyPropertyChanged
    {
        protected const string cmdString = "Command";

        private bool isSyncing;
        protected readonly INotifyPropertyChangedHelper helper;
        protected readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>();
        protected readonly Dictionary<Guid, IPlaylistBase> playlists = new Dictionary<Guid, IPlaylistBase>();
        private readonly Dictionary<Guid, InitList<string>> initPlaylistLists = new Dictionary<Guid, InitList<string>>();

        public abstract bool IsOpen { get; }

        public bool IsSyncing
        {
            get => isSyncing;
            protected set
            {
                if (value == isSyncing) return;

                isSyncing = value;
                OnPropertyChanged(nameof(IsSyncing));
            }
        }

        public IAudioServiceBase Service { get; protected set; }

        protected MqttCommunicator(INotifyPropertyChangedHelper helper = null)
        {
            this.helper = helper;
        }

        protected async void InitPlaylists()
        {
            foreach (IPlaylistBase playlist in (Service?.Playlists).ToNotNull())
            {
                await AddPlaylist(playlist);
            }
        }

        public abstract Task SetService(IAudioServiceBase service, BuildStatusToken statusToken);

        public abstract Task SyncService(BuildStatusToken statusToken);

        protected void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged += Service_AudioDataChanged;
            service.AudioFormatChanged += Service_AudioFormatChanged;
            service.CurrentPlaylistChanged += Service_CurrentPlaylistChanged;
            service.PlaylistsChanged += Service_PlaylistsChanged;
            service.PlayStateChanged += Service_PlayStateChanged;
            service.VolumeChanged += Service_VolumeChanged;

            Subscribe(service.SourcePlaylist);
            Subscribe(service.Playlists);
        }

        protected void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged -= Service_AudioDataChanged;
            service.AudioFormatChanged -= Service_AudioFormatChanged;
            service.CurrentPlaylistChanged -= Service_CurrentPlaylistChanged;
            service.PlaylistsChanged -= Service_PlaylistsChanged;
            service.PlayStateChanged -= Service_PlayStateChanged;
            service.VolumeChanged -= Service_VolumeChanged;

            Unsubscribe(service.SourcePlaylist);
            Unsubscribe(service.Playlists);
        }

        private void Subscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Subscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged += Playlist_FileMediaSourcesChanged;
            playlist.IsSearchShuffleChanged += Playlist_IsSearchShuffleChanged;
            playlist.SearchKeyChanged += Playlist_SearchKeyChanged;
        }

        private void Unsubscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Unsubscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged -= Playlist_FileMediaSourcesChanged;
            playlist.IsSearchShuffleChanged -= Playlist_IsSearchShuffleChanged;
            playlist.SearchKeyChanged -= Playlist_SearchKeyChanged;
        }

        private void Subscribe(IEnumerable<IPlaylistBase> playlists)
        {
            foreach (IPlaylistBase playlist in playlists ?? Enumerable.Empty<IPlaylistBase>()) Subscribe(playlist);
        }

        private void Unsubscribe(IEnumerable<IPlaylistBase> playlists)
        {
            foreach (IPlaylistBase playlist in playlists ?? Enumerable.Empty<IPlaylistBase>()) Unsubscribe(playlist);
        }

        private void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            System.Diagnostics.Debug.WriteLine("Subscribe: " + playlist.ID);
            playlist.CurrentSongChanged += Playlist_CurrentSongChanged;
            playlist.DurationChanged += Playlist_DurationChanged;
            playlist.IsAllShuffleChanged += Playlist_IsAllShuffleChanged;
            playlist.LoopChanged += Playlist_LoopChanged;
            playlist.PositionChanged += Playlist_PositionChanged;
            playlist.SongsChanged += Playlist_SongsChanged;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            System.Diagnostics.Debug.WriteLine("Unsubscribe: " + playlist.ID);
            playlist.CurrentSongChanged -= Playlist_CurrentSongChanged;
            playlist.DurationChanged -= Playlist_DurationChanged;
            playlist.IsAllShuffleChanged -= Playlist_IsAllShuffleChanged;
            playlist.LoopChanged -= Playlist_LoopChanged;
            playlist.PositionChanged -= Playlist_PositionChanged;
            playlist.SongsChanged -= Playlist_SongsChanged;
        }

        protected abstract Task SubscribeAsync(IPlaylistBase playlist);

        private async void Service_AudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
            await PublishAudioData();
        }

        protected async Task PublishAudioData()
        {
            await PublishAsync(nameof(Service.AudioData), Service.AudioData, MqttQualityOfServiceLevel.AtMostOnce);
        }

        private async void Service_AudioFormatChanged(object sender, ValueChangedEventArgs<WaveFormat> e)
        {
            await PublishFormat();
        }

        protected async Task PublishFormat()
        {
            ByteQueue data = new ByteQueue();
            if (Service.AudioFormat != null) data.Enqueue(Service.AudioFormat);

            await PublishAsync(nameof(Service.AudioFormat), data);
        }

        private async void Service_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            await PublishCurrentPlaylist();
        }

        protected async Task PublishCurrentPlaylist()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.CurrentPlaylist.ID);

            await Task.WhenAll(PublishAsync(nameof(Service.CurrentPlaylist), data), AddPlaylist(Service.CurrentPlaylist));
        }

        private async void Service_PlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
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
                PublishDuration(playlist), PublishSongs(playlist), PublishCurrentSong(playlist));
        }

        private async void Service_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            await PublishPlayState();
        }

        protected async Task PublishPlayState()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)Service.PlayState);

            await PublishAsync(nameof(Service.PlayState), data);
        }

        private async void Service_VolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            await PublishVolume();
        }

        protected async Task PublishVolume()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.Volume);

            await PublishAsync(nameof(Service.Volume), data, MqttQualityOfServiceLevel.AtMostOnce);
        }

        private async void Playlist_FileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
        {
            await PublishMediaSources((ISourcePlaylistBase)sender);
        }

        protected async Task PublishMediaSources(ISourcePlaylistBase source)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(source.FileMediaSources);

            await PublishAsync(source, nameof(source.FileMediaSources), data);
        }

        private async void Playlist_IsSearchShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
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

        private async void Playlist_SearchKeyChanged(object sender, ValueChangedEventArgs<string> e)
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

        private async void Playlist_CurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            await PublishCurrentSong((IPlaylistBase)sender);
        }

        protected async Task PublishCurrentSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            if (playlist.CurrentSong.HasValue) data.Enqueue(playlist.CurrentSong.Value);
            else data.Enqueue(false);

            await PublishAsync(playlist, nameof(playlist.CurrentSong), data);
        }

        private async void Playlist_DurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishDuration((IPlaylistBase)sender);
        }

        protected async Task PublishDuration(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Duration);

            await PublishAsync(playlist, nameof(playlist.Duration), data);
        }

        private async void Playlist_IsAllShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            await PublishIsAllShuffle((IPlaylistBase)sender);
        }

        protected async Task PublishIsAllShuffle(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.IsAllShuffle);

            await PublishAsync(playlist, nameof(playlist.IsAllShuffle), data);
        }

        private async void Playlist_LoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
            await PublishLoop((IPlaylistBase)sender);
        }

        protected async Task PublishLoop(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Loop);

            await PublishAsync(playlist, nameof(playlist.Loop), data);
        }

        private async void Playlist_PositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishPosition((IPlaylistBase)sender);
        }

        protected async Task PublishPosition(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Position);

            await PublishAsync(playlist, nameof(playlist.Position), data);
        }

        private async void Playlist_SongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            await PublishSongs((IPlaylistBase)sender);
        }

        protected async Task PublishSongs(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Songs);

            await PublishAsync(playlist, nameof(playlist.Songs), data);
        }

        public abstract Task CloseAsync();

        public abstract Task OpenAsync(BuildStatusToken statusToken);

        public async Task SendCommand(string cmd)
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

            ByteQueue data = payload;

            if (ContainsPlaylist(rawTopic, out topic, out id))
            {
                bool handled = HandleMessage(id, topic, data);

                InitList<string> initPlaylistList;
                if (initPlaylistLists.TryGetValue(id, out initPlaylistList)) initPlaylistList?.Remove(rawTopic);

                return handled;
            }

            switch (topic)
            {
                case nameof(Service.Playlists):
                    Task<IPlaylistBase>[] tasks = data.DequeueGuids().Select(guid => GetInitPlaylist(guid)).ToArray();

                    await Task.WhenAll(tasks);

                    Service.Playlists = tasks.Select(t => t.Result).ToArray();
                    break;

                case nameof(Service.AudioData):
                    Service.AudioData = data;
                    break;

                case nameof(Service.CurrentPlaylist):
                    Service.CurrentPlaylist = await GetInitPlaylist(data.DequeueGuid());
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

        private bool HandleMessage(Guid id, string topic, ByteQueue data)
        {
            IPlaylistBase playlist = GetPlaylist(id);
            ISourcePlaylistBase source = playlist as ISourcePlaylistBase;

            switch (topic)
            {
                case nameof(playlist.CurrentSong):
                    playlist.CurrentSong = data.Count() > 1 ? (Song?)data.DequeueSong() : null;
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

        public bool ContainsPlaylist(string rawTopic, out string topic, out Guid id)
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

        private IPlaylistBase GetPlaylist(Guid id)
        {
            if (id == Guid.Empty) return Service.SourcePlaylist;

            IPlaylistBase playlist;
            if (!playlists.TryGetValue(id, out playlist))
            {
                playlist = new Playlist(id, helper);

                InitPlaylist(playlist);
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

            await InitPlaylist(playlist);

            return playlist;
        }

        private async Task InitPlaylist(IPlaylistBase playlist)
        {
            System.Diagnostics.Debug.WriteLine("InitPlaylist1: " + playlist.ID);
            await AddPlaylist(playlist, false);

            InitList<string> initPlaylistList = new InitList<string>(GetTopics(playlist));
            initPlaylistLists.Add(playlist.ID, initPlaylistList);

            await initPlaylistList.Task;

            initPlaylistLists.Remove(playlist.ID);

            System.Diagnostics.Debug.WriteLine("InitPlaylist2: " + playlist.ID);
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void Dispose();

        public void LockTopic(string topic, byte[] payload)
        {
            LockTopic(receivingDict, topic, payload);
        }

        public static void LockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
            byte[] payloadLock;

            while (true)
            {
                lock (dict)
                {
                    if (!dict.TryGetValue(topic, out payloadLock))
                    {
                        dict.Add(topic, payload);
                        return;
                    }
                }

                lock (payloadLock) Monitor.Wait(payloadLock);
            }
        }

        public bool IsTopicLocked(string topic, byte[] payload)
        {
            return IsTopicLocked(receivingDict, topic, payload);
        }

        public static bool IsTopicLocked(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
            byte[] payloadLock;

            if (!dict.TryGetValue(topic, out payloadLock)) return false;

            return payload.BothNullOrSequenceEqual(payloadLock);
        }

        public bool UnlockTopic(string topic, bool pulseAll = false)
        {
            return UnlockTopic(receivingDict, topic, pulseAll);
        }

        public static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, bool pulseAll = false)
        {
            byte[] payloadLock;

            lock (dict)
            {
                if (!dict.TryGetValue(topic, out payloadLock)) return false;

                dict.Remove(topic);
            }

            lock (payloadLock)
            {
                if (pulseAll) Monitor.PulseAll(payloadLock);
                else Monitor.Pulse(payloadLock);
            }

            return true;
        }

        public bool UnlockTopic(string topic, byte[] payload, bool pulseAll = false)
        {
            return UnlockTopic(receivingDict, topic, payload, pulseAll);
        }

        public static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload, bool pulseAll = false)
        {
            byte[] payloadLock;

            lock (dict)
            {
                if (!dict.TryGetValue(topic, out payloadLock) || payloadLock.BothNullOrSequenceEqual(payload)) return false;

                dict.Remove(topic);
            }

            lock (payloadLock)
            {
                if (pulseAll) Monitor.PulseAll(payloadLock);
                else Monitor.Pulse(payloadLock);
            }

            return true;
        }
    }
}
