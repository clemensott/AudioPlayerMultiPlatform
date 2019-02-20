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

        private bool isStreaming, isOpenning;
        private List<string> initProps;
        private readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>(),
            latestValueDict = new Dictionary<string, byte[]>();
        private readonly PublishQueue publishQueue = new PublishQueue();
        private readonly Queue<MqttApplicationMessage> receiveMessages = new Queue<MqttApplicationMessage>();
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

        public bool IsOpenning
        {
            get { return isOpenning; }
            private set
            {
                if (value == isOpenning) return;

                isOpenning = value;
                OnPropertyChanged(nameof(IsOpenning));
            }
        }

        public bool IsOpen { get { return client?.IsConnected ?? false; } }

        public override IPlayer Player { get { return player; } }

        public IMqttClient MqttClient => client;

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
            try
            {
                IsOpenning = true;

                IEnumerable<string> serviceTopics = GetTopicFilters().Select(tf => tf.Topic);
                IEnumerable<string> fileBasePlaylistTopics = GetTopicFilters(FileBasePlaylist).Select(tf => tf.Topic);
                initProps = serviceTopics.Concat(fileBasePlaylistTopics).ToList();

                await client.ConnectAsync(ServerAddress, Port);

                Task.Run(ProcessPublish);
                Task.Run(ProcessReceive);

                await Task.WhenAll(GetTopicFilters().Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));
                await Task.WhenAll(GetTopicFilters(FileBasePlaylist).Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));

                await Utils.WaitAsync(initProps, () => initProps.Count > 0);
            }
            catch
            {
                try
                {
                    await CloseAsync();
                }
                catch { }

                throw;
            }
            finally
            {
                initProps = null;

                IsOpenning = false;
            }
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

        private async Task ProcessPublish()
        {
            while (IsOpen)
            {
                try
                {
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish1");
                    ////if (!client.IsConnected)  await OpenAsync();

                    //DateTime time = DateTime.Now;
                    //while (time + TimeSpan.FromMilliseconds(100) > DateTime.Now) ;

                    System.Diagnostics.Debug.WriteLine("ConsumerPublish2");
                    currentPublish = publishQueue.Dequeue();

                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish3");
                    //time = DateTime.Now;
                    //while (time + TimeSpan.FromMilliseconds(100) > DateTime.Now) ;

                    Task waitForReply = Utils.WaitAsync(currentPublish);
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish4");
                    //Task waitForTimeOut = Task.Delay(timeout);
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish5");

                    await client.PublishAsync(currentPublish);
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish6");

                    await Task.WhenAny(waitForReply, Task.Delay(timeout));
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish7");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("ConsumerPublishException:\r\n" + e);
                }

                //System.Diagnostics.Debug.WriteLine("ConsumerPublish8");
                MqttApplicationMessage message = currentPublish;
                System.Diagnostics.Debug.WriteLine("ConsumerPublish9: " + (message == null));

                if (currentPublish == null) continue;

                System.Diagnostics.Debug.WriteLine("ConsumerPublish10");

                lock (message)
                {
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish11");
                    currentPublish = null;
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish12");

                    if (!publishQueue.IsEnqueued(message.Topic))
                    {
                        System.Diagnostics.Debug.WriteLine("ConsumerPublish13");
                        publishQueue.Enqueue(message);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ConsumerPublish14");
                        Monitor.PulseAll(message);
                    }

                    System.Diagnostics.Debug.WriteLine("ConsumerPublish15");
                }
            }

            System.Diagnostics.Debug.WriteLine("ConsumerPublish16");
        }

        private void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            string rawTopic = e.ApplicationMessage.Topic;
            byte[] payload = e.ApplicationMessage.Payload;

            if (currentPublish?.Topic == rawTopic && currentPublish.Payload.SequenceEqual(payload))
            {
                lock (currentPublish)
                {
                    Monitor.PulseAll(currentPublish);
                    currentPublish = null;

                    System.Diagnostics.Debug.WriteLine("Client_ApplicationMessageReceived1");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("Client_ApplicationMessageReceived2");

            lock (receiveMessages)
            {
                receiveMessages.Enqueue(e.ApplicationMessage);

                Monitor.Pulse(receiveMessages);
            }

            System.Diagnostics.Debug.WriteLine("Client_ApplicationMessageReceived3");
        }

        private async Task ProcessReceive()
        {
            while (IsOpen)
            {
                try
                {
                    MqttApplicationMessage message;

                    lock (receiveMessages)
                    {
                        while (receiveMessages.Count == 0) Monitor.Wait(receiveMessages);

                        message = receiveMessages.Dequeue();
                    }

                    await ProcessApplicationMessage(message);
                }
                catch { }
            }
        }

        private async Task ProcessApplicationMessage(MqttApplicationMessage e)
        {
            string rawTopic = e.Topic;
            byte[] payload = e.Payload;

            System.Diagnostics.Debug.WriteLine("ProcessApplicationMessage1");
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
            System.Diagnostics.Debug.WriteLine("ProcessApplicationMessage4");
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
