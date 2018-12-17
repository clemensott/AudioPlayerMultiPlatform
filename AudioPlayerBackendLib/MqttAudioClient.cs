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
        private bool isStreaming;
        private List<string> initProps;
        private readonly List<(string topic, byte[] payload)> receivingTuples;
        private readonly Dictionary<string, byte[]> sendingTuples;
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

            receivingTuples = new List<(string topic, byte[] payload)>();
            sendingTuples = new Dictionary<string, byte[]>();

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
            initProps = GetTopicFilters().Select(tf => tf.Topic).ToList();

            await client.ConnectAsync(ServerAddress, Port);
            await Task.WhenAll(GetTopicFilters().Select(tf => client.SubscribeAsync(tf.Topic, tf.Qos)));

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

            yield return new TopicFilter(nameof(AllSongsShuffled), qos);
            yield return new TopicFilter(nameof(CurrentSong), qos);
            yield return new TopicFilter(nameof(Duration), qos);
            yield return new TopicFilter(nameof(IsAllShuffle), qos);
            yield return new TopicFilter(nameof(IsOnlySearch), qos);
            yield return new TopicFilter(nameof(IsSearchShuffle), qos);
            yield return new TopicFilter(nameof(MediaSources), qos);
            yield return new TopicFilter(nameof(PlayState), qos);
            yield return new TopicFilter(nameof(Position), qos);
            yield return new TopicFilter(nameof(SearchKey), qos);
            yield return new TopicFilter(nameof(Volume), qos);
        }

        private async void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            byte[] payload = e.ApplicationMessage.Payload;

            if (sendingTuples.TryGetValue(topic, out byte[] dictPayload) && dictPayload.SequenceEqual(payload))
            {
                lock (dictPayload)
                {
                    sendingTuples.Remove(topic);
                    Monitor.PulseAll(dictPayload);
                }

                return;
            }

            receivingTuples.Add((topic, payload));

            ByteQueue queue = payload;

            try
            {
                switch (topic)
                {
                    case nameof(AllSongsShuffled):
                        AllSongsShuffled = queue.DequeueSongs();
                        break;

                    case nameof(AudioData):
                        AudioData = queue;
                        break;

                    case nameof(CurrentSong):
                        CurrentSong = queue.DequeueSong();
                        break;

                    case nameof(Duration):
                        Duration = queue.DequeueTimeSpan();
                        break;

                    case nameof(Format):
                        Format = queue.DequeueWaveFormat();
                        break;

                    case nameof(IsAllShuffle):
                        IsAllShuffle = queue.DequeueBool();
                        break;

                    case nameof(IsOnlySearch):
                        IsOnlySearch = queue.DequeueBool();
                        break;

                    case nameof(IsSearchShuffle):
                        IsSearchShuffle = queue.DequeueBool();
                        break;

                    case nameof(MediaSources):
                        MediaSources = queue.DequeueStrings();
                        break;

                    case nameof(PlayState):
                        PlayState = queue.DequeuePlayState();
                        break;

                    case nameof(Position):
                        Position = queue.DequeueTimeSpan();
                        break;

                    case nameof(SearchKey):
                        SearchKey = queue.DequeueString();
                        break;

                    case nameof(Volume):
                        Volume = queue.DequeueFloat();
                        break;
                }
            }
            catch (Exception exc)
            {
                try
                {
                    MqttApplicationMessage message = new MqttApplicationMessage()
                    {
                        Topic = "Debug",
                        Payload = Encoding.UTF8.GetBytes(Utils.GetTypeMessageAndStack(exc)),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = true
                    };

                    await client.PublishAsync(message);
                }
                catch { }
            }

            if (initProps != null && initProps.Contains(topic))
            {
                lock (initProps)
                {
                    initProps.Remove(topic);

                    Monitor.Pulse(initProps);
                }
            }

            receivingTuples.Remove((topic, payload));
        }

        private async Task Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (receivingTuples.Contains((topic, payload))) return;

            MqttApplicationMessage message = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            };

            byte[] dictPayload;
            while (sendingTuples.TryGetValue(topic, out dictPayload)) await Utils.WaitAsync(dictPayload);

            sendingTuples.Add(topic, payload);

            try
            {
                if (!client.IsConnected) await OpenAsync();

                await client.PublishAsync(message);
            }
            catch { }
        }

        protected async override void OnCurrentSongChanged()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentSong.HasValue) queue.Enqueue(CurrentSong.Value);

            await Publish(nameof(CurrentSong), queue);
        }

        protected async override void OnIsAllShuffleChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsAllShuffle);

            await Publish(nameof(IsAllShuffle), queue);
        }

        protected async override void OnIsOnlySearchChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsOnlySearch);

            await Publish(nameof(IsOnlySearch), queue);
        }

        protected async override void OnIsSearchShuffleChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsSearchShuffle);

            await Publish(nameof(IsSearchShuffle), queue);
        }

        protected async override void OnMediaSourcesChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(MediaSources);

            await Publish(nameof(MediaSources), queue);
        }

        protected async override void OnPlayStateChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(PlayState);

            await Publish(nameof(PlayState), queue);
        }

        protected async override void OnPositionChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Position);

            await Publish(nameof(Position), queue);
        }

        protected async override void OnSearchKeyChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(SearchKey);

            await Publish(nameof(SearchKey), queue);
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

        protected override void OnServiceVolumeChanged()
        {
            PublishServiceVolume();
        }

        private async void PublishServiceVolume()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Volume);

            await Publish(nameof(Volume), queue);
        }

        public async override void Reload()
        {
            await Publish(nameof(AllSongsShuffled), new byte[0]);
        }

        protected override void OnDurationChanged()
        {
        }

        protected override void OnAllSongsShuffledChanged()
        {
        }

        public async override void Dispose()
        {
            if (IsOpen) await CloseAsync();

            player.Stop(buffer);
        }
    }
}
