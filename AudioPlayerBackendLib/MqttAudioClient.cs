using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public class MqttAudioClient : AudioClient, IMqttAudioClient
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        private bool isStreaming;
        private List<string> initProps;
        private readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>(),
            latestValueDict = new Dictionary<string, byte[]>();
        private readonly PublishQueue publishQueue = new PublishQueue();
        private MqttApplicationMessage currentPublish;
        private readonly IMqttClient client;
        private readonly IPlayer player;
        private readonly IMqttAudioClientHelper helper;
        private IBufferedWaveProvider buffer;

        public bool IsStreaming
        {
            get { return isStreaming; }
            set
            {
                if (value == isStreaming) return;

                isStreaming = value;

                if (value)
                {
                    client.SubscribeAsync(nameof(Format), MqttQualityOfServiceLevel.AtLeastOnce);
                    client.SubscribeAsync(nameof(AudioData), MqttQualityOfServiceLevel.AtMostOnce);
                }
                else
                {
                    client.UnsubscribeAsync(nameof(AudioData));
                    client.UnsubscribeAsync(nameof(Format));
                }

                OnPropertyChanged(nameof(IsStreaming));
            }
        }

        public float ClientVolume
        {
            get { return player.Volume; }
            set
            {
                if (value == player.Volume) return;

                player.Volume = value;
                OnPropertyChanged(nameof(ClientVolume));
            }
        }

        public string ServerAddress { get; private set; }

        public int? Port { get; private set; }

        public bool IsOpen { get { return client?.IsConnected ?? false; } }

        public override IPlayer Player { get { return player; } }

        private MqttAudioClient(IPlayer player, IMqttAudioClientHelper helper) : base(helper)
        {
            this.player = player;
            this.helper = helper;

            client = CreateMqttClient();
            client.ApplicationMessageReceived += Client_ApplicationMessageReceived;
        }

        public MqttAudioClient(IPlayer player, string server, int? port = null, IMqttAudioClientHelper helper = null) : this(player, helper)
        {
            this.helper = helper;
            ServerAddress = server;
            Port = port;
        }

        protected virtual IMqttClient CreateMqttClient()
        {
            return helper.CreateMqttClient(this);
        }

        public async Task OpenAsync()
        {
            IEnumerable<string> serviceTopics = GetTopicFilters().Select(tf => tf.Topic);
            IEnumerable<string> fileBasePlaylistTopics = GetTopicFilters(FileBasePlaylist).Select(tf => tf.Topic);
            initProps = serviceTopics.Concat(fileBasePlaylistTopics).ToList();

            await client.ConnectAsync(ServerAddress, Port);

            Task.Run(new Action(ConsumerPublish));

            await Task.WhenAll(GetTopicFilters().Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));
            await Task.WhenAll(GetTopicFilters(FileBasePlaylist).Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));

            await Utils.WaitAsync(initProps, () => initProps.Count > 0);

            initProps = null;
        }

        public async Task CloseAsync()
        {
            await Task.WhenAll(GetTopicFilters().Select(tf => client.UnsubscribeAsync(tf.Topic)));

            await client.UnsubscribeAsync(nameof(Format));
            await client.UnsubscribeAsync(nameof(AudioData));
            await client.DisconnectAsync();
        }

        private IEnumerable<TopicFilter> GetTopicFilters()
        {
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return new TopicFilter(nameof(AdditionalPlaylists), qos);
            yield return new TopicFilter(nameof(CurrentPlaylist), qos);
            yield return new TopicFilter(nameof(FileMediaSources), qos);
            yield return new TopicFilter(nameof(PlayState), qos);
            yield return new TopicFilter(nameof(Volume), qos);

        }

        private IEnumerable<TopicFilter> GetTopicFilters(IPlaylist playlist)
        {
            string id = playlist.ID + ".";
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return new TopicFilter(id + nameof(playlist.CurrentSong), qos);
            yield return new TopicFilter(id + nameof(playlist.Songs), qos);
            yield return new TopicFilter(id + nameof(playlist.Duration), qos);
            yield return new TopicFilter(id + nameof(playlist.IsAllShuffle), qos);
            yield return new TopicFilter(id + nameof(playlist.IsOnlySearch), qos);
            yield return new TopicFilter(id + nameof(playlist.IsSearchShuffle), qos);
            yield return new TopicFilter(id + nameof(playlist.Loop), qos);
            yield return new TopicFilter(id + nameof(playlist.Position), qos);
            yield return new TopicFilter(id + nameof(playlist.SearchKey), qos);
        }

        private async void ConsumerPublish()
        {
            while (IsOpen)
            {
                try
                {
                    if (!client.IsConnected)
                    {
                        //await OpenAsync();
                    }

                    System.Diagnostics.Debug.WriteLine("Publish0");
                    await Task.Delay(100);

                    System.Diagnostics.Debug.WriteLine("Publish1");
                    currentPublish = publishQueue.Dequeue();

                    System.Diagnostics.Debug.WriteLine("Publish2: " + currentPublish?.Topic);
                    Task waitForReply = Utils.WaitAsync(currentPublish);
                    Task waitForTimeOut = Task.Delay(timeout);

                    System.Diagnostics.Debug.WriteLine("Publish3: " + currentPublish?.Topic);
                    await client.PublishAsync(currentPublish);
                    System.Diagnostics.Debug.WriteLine("Publish4: " + currentPublish?.Topic);
                    await Task.WhenAny(waitForReply, waitForTimeOut);
                    System.Diagnostics.Debug.WriteLine("Publish5: " + currentPublish?.Topic);
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }

                MqttApplicationMessage message = currentPublish;

                if (currentPublish == null) continue;

                lock (message)
                {
                    System.Diagnostics.Debug.WriteLine("Timeout: " + message?.Topic);

                    currentPublish = null;

                    if (!publishQueue.IsEnqueued(message.Topic)) publishQueue.Enqueue(message);
                    else Monitor.PulseAll(message);
                }
            }
        }

        private async void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            string rawTopic = e.ApplicationMessage.Topic;
            byte[] payload = e.ApplicationMessage.Payload;

            if (currentPublish != null && currentPublish.Topic == rawTopic && currentPublish.Payload.SequenceEqual(payload))
            {
                lock (currentPublish)
                {
                    Monitor.PulseAll(currentPublish);
                    currentPublish = null;
                }
            }

            MqttAudioUtils.LockTopic(receivingDict, rawTopic, payload);

            try
            {
                string topic;
                IPlaylist playlist;
                if (MqttAudioUtils.ContainsPlaylist(this, rawTopic, out topic, out playlist))
                {
                    MqttAudioUtils.TryHandleMessage(this, topic, payload, playlist);
                }
                else MqttAudioUtils.TryHandleMessage(this, topic, payload);
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);

                try
                {
                    MqttApplicationMessage message = new MqttApplicationMessage()
                    {
                        Topic = "Debug",
                        Payload = Encoding.UTF8.GetBytes(exc.ToString()),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = true
                    };

                    await client.PublishAsync(message);
                }
                catch { }
            }

            if (initProps != null && initProps.Contains(rawTopic))
            {
                lock (initProps)
                {
                    initProps.Remove(rawTopic);

                    Monitor.Pulse(initProps);
                }
            }

            MqttAudioUtils.UnlockTopic(receivingDict, rawTopic);
        }

        private async Task Publish(IPlaylist playlist, string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            await Publish(playlist.ID + "." + topic, payload, qos, retain);
        }

        private async Task Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (!IsOpen || MqttAudioUtils.IsTopicLocked(receivingDict, topic, payload)) return;

            MqttApplicationMessage message = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            };

            publishQueue.Enqueue(message);

            await Utils.WaitAsync(message);

            //bool isSearchKey = topic.EndsWith(".SearchKey");
            //string searchKey = isSearchKey ? Encoding.UTF8.GetString(payload.Skip(4).ToArray()) : string.Empty;

            //if (client == null || MqttAudioUtils.IsTopicLocked(receivingDict, topic, payload)) return;

            //System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Latest (" + searchKey + "): " + latestValueDict.ContainsKey(topic));
            //if (latestValueDict.ContainsKey(topic))
            //{
            //    latestValueDict[topic] = payload;
            //    return;
            //}
            //else latestValueDict.Add(topic, payload);

            //byte[] dictPayload;
            //while (sendingDict.TryGetValue(topic, out dictPayload))
            //{
            //    System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Wait1 (" + searchKey + "): " + dictPayload.GetHashCode());
            //    await Utils.WaitAsync(dictPayload);

            //    //System.Diagnostics.Debug.WriteLineIf(isSearchKey && searchKey == latestSearchKey,
            //    //    "Wait2 (" + searchKey + "): " + dictPayload.GetHashCode());

            //    //if (latestSendDict[topic] != payload)
            //    //{
            //    //    //System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Skip (" + searchKey + "): " + latestSearchKey);
            //    //    return;
            //    //}
            //}

            ////System.Diagnostics.Debug.WriteLineIf(isSearchKey, "NotSkip: " + searchKey);

            //payload = latestValueDict[topic];
            //latestValueDict.Remove(topic);

            //searchKey = isSearchKey ? Encoding.UTF8.GetString(payload.Skip(4).ToArray()) : string.Empty;

            //sendingDict.Add(topic, payload);
            //System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Add (" + searchKey + "): " + payload.GetHashCode());

            //try
            //{
            //    Task.Run(Publish);
            //}
            //catch { }

            //async Task Publish()
            //{
            //    if (!client.IsConnected) await OpenAsync();

            //    await client.PublishAsync(message);

            //    System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Send: " + searchKey);
            //    Task waitForReply = Utils.WaitAsync(payload);
            //    Task waitForTimeOut = Task.Delay(timeout);

            //    await Task.WhenAny(waitForReply, waitForTimeOut);
            //    System.Diagnostics.Debug.WriteLineIf(isSearchKey, "Sent: " + searchKey);

            //    lock (payload)
            //    {
            //        if (!waitForReply.IsCompleted)
            //        {
            //            System.Diagnostics.Debug.WriteLineIf(isSearchKey, "TimeOut: " + searchKey);
            //            sendingDict.Remove(topic);
            //            Monitor.PulseAll(payload);
            //        }
            //    }
            //}
        }

        protected async override void OnAddPlaylist(IPlaylist playlist)
        {
            if (initProps != null)
            {
                initProps.AddRange(GetTopicFilters(playlist).Select(tp => playlist.ID + "." + tp.Topic));
            }

            await Task.WhenAll(GetTopicFilters(playlist).Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));

            await PublishAdditionalPlaylists();
        }

        protected async override void OnRemovePlaylist(IPlaylist playlist)
        {
            if (initProps != null)
            {
                foreach (string topic in GetTopicFilters(playlist).Select(tp => playlist.ID + "." + tp.Topic))
                {
                    initProps.Remove(topic);
                }
            }

            await Task.WhenAll(GetTopicFilters(playlist).Select(tf => client.UnsubscribeAsync(tf.Topic)));

            await PublishAdditionalPlaylists();
        }

        private async Task PublishAdditionalPlaylists()
        {
            if (receivingDict.ContainsKey(nameof(AdditionalPlaylists))) return;

            ByteQueue queue = new ByteQueue();
            queue.Enqueue(AdditionalPlaylists);

            await Publish(nameof(AdditionalPlaylists), queue);
        }

        protected async override void OnCurrenPlaylistChanged()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentPlaylist != null) queue.EnqueueRange(CurrentPlaylist.ID.ToByteArray());

            await Publish(nameof(CurrentPlaylist), queue);
        }

        protected async override void OnCurrentSongChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            if (playlist.CurrentSong.HasValue) queue.Enqueue(playlist.CurrentSong.Value);

            await Publish(playlist, nameof(playlist.CurrentSong), queue);
        }

        protected async override void OnDurationChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Duration);

            await Publish(playlist, nameof(playlist.Duration), queue);
        }

        protected async override void OnLoopChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue((int)playlist.Loop);

            await Publish(playlist, nameof(playlist.Loop), queue);
        }

        protected async override void OnIsAllShuffleChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsAllShuffle);

            await Publish(playlist, nameof(playlist.IsAllShuffle), queue);
        }

        protected async override void OnIsOnlySearchChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsOnlySearch);

            await Publish(playlist, nameof(playlist.IsOnlySearch), queue);
        }

        protected async override void OnIsSearchShuffleChangedAsync(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsSearchShuffle);

            await Publish(playlist, nameof(playlist.IsSearchShuffle), queue);
        }

        protected async override void OnMediaSourcesChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(FileMediaSources);

            await Publish(nameof(FileMediaSources), queue);
        }

        protected async override void OnPlayStateChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue((int)PlayState);

            await Publish(nameof(PlayState), queue);
        }

        protected async override void OnPositionChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Position);

            await Publish(playlist, nameof(playlist.Position), queue);
        }

        protected async override void OnSearchKeyChanged(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.SearchKey);

            await Publish(playlist, nameof(playlist.SearchKey), queue);
        }

        protected async override void OnSongsChanged(IPlaylist playlist)
        {
            await PublishSongs(playlist);
        }

        private async Task PublishSongs(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Songs);

            await Publish(playlist, nameof(playlist.Songs), queue);
        }

        protected override void OnFormatChanged()
        {
            player.Play(GetBufferedWaveProvider);
        }

        private IWaveProvider GetBufferedWaveProvider()
        {
            if (buffer == null) buffer = CreateBufferedWaveProvider(Format);
            else if (buffer.WaveFormat != Format)
            {
                buffer.ClearBuffer();
                buffer = CreateBufferedWaveProvider(Format);
            }

            return buffer;
        }

        protected virtual IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format)
        {
            return helper.CreateBufferedWaveProvider(format, this);
        }

        protected override void OnAudioDataChanged()
        {
            if (buffer != null) buffer.AddSamples(AudioData, 0, AudioData.Length);

            player.PlayState = PlaybackState.Playing;
        }

        protected async override void OnServiceVolumeChanged()
        {
            await PublishServiceVolume();
        }

        private async Task PublishServiceVolume()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Volume);

            await Publish(nameof(Volume), queue);
        }

        public async override void Reload()
        {
            await Publish(nameof(FileBasePlaylist.Songs), FileBasePlaylist.ID.ToByteArray());
        }

        public async override void Dispose()
        {
            if (IsOpen) await CloseAsync();

            player.Stop(buffer);
        }
    }
}
