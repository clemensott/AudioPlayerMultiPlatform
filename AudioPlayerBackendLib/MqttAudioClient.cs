﻿using AudioPlayerBackend.Common;
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
        private readonly List<string> messageReceivingTopics;
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

            messageReceivingTopics = new List<string>();

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
            ByteQueue queue = e.ApplicationMessage.Payload;

            messageReceivingTopics.Add(topic);

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

            messageReceivingTopics.Remove(topic);
        }

        private void Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (messageReceivingTopics.Contains(topic)) return;

            MqttApplicationMessage message = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            };

            if (!client.IsConnected) OpenAsync().Wait();

            client.PublishAsync(message);
        }

        protected override void OnCurrentSongChanged()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentSong.HasValue) queue.Enqueue(CurrentSong.Value);

            Publish(nameof(CurrentSong), queue);
        }

        protected override void OnIsAllShuffleChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsAllShuffle);

            Publish(nameof(IsAllShuffle), queue);
        }

        protected override void OnIsOnlySearchChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsOnlySearch);

            Publish(nameof(IsOnlySearch), queue);
        }

        protected override void OnIsSearchShuffleChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsSearchShuffle);

            Publish(nameof(IsSearchShuffle), queue);
        }

        protected override void OnMediaSourcesChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(MediaSources);

            Publish(nameof(MediaSources), queue);
        }

        protected override void OnPlayStateChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(PlayState);

            Publish(nameof(PlayState), queue);
        }

        protected override void OnPositionChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Position);

            Publish(nameof(Position), queue);
        }

        protected override void OnSearchKeyChanged()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(SearchKey);

            Publish(nameof(SearchKey), queue);
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

        private void PublishServiceVolume()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Volume);

            Publish(nameof(Volume), queue);
        }

        public override void Reload()
        {
            Publish(nameof(AllSongsShuffled), new byte[0]);
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
