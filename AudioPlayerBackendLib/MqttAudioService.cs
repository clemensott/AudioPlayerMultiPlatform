using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public abstract class MqttAudioService : AudioService, IMqttAudioService
    {
        private readonly List<string> messageInterceptingTopics;
        private readonly IMqttServer server;
        private ReadEventWaveProvider waveProvider;

        public int Port { get; private set; }

        public bool IsOpen { get; private set; }

        public MqttAudioService(IPlayer player, int port) : base(player)
        {
            messageInterceptingTopics = new List<string>();
            server = CreateMqttServer();

            Port = port;
        }

        protected abstract IMqttServer CreateMqttServer();

        public async Task OpenAsync()
        {
            IsOpen = true;

            await server.StartAsync(Port, OnApplicationMessageInterception);

            PublishAllSongsShuffled();
            PublishCurrentSong();
            PublishDuration();
            PublishIsAllShuffle();
            PublishIsOnlySearch();
            PublishIsSearchShuffle();
            PublishMediaSources();
            PublishPlayState();
            PublishPosition();
            PublishSearchKey();
            PublishServiceVolume();
        }

        public async Task CloseAsync()
        {
            IsOpen = false;

            await server.StopAsync();
        }

        private async void OnApplicationMessageInterception(MqttApplicationMessageInterceptorContext context)
        {
            if (context.ClientId == null) return;

            messageInterceptingTopics.Add(context.ApplicationMessage.Topic);

            ByteQueue queue = context.ApplicationMessage.Payload;

            try
            {
                switch (context.ApplicationMessage.Topic)
                {
                    case nameof(AllSongsShuffled):
                        Reload();

                        queue = new ByteQueue();
                        queue.Enqueue(AllSongsShuffled);
                        context.ApplicationMessage.Payload = queue;
                        break;

                    case nameof(CurrentSong):
                        CurrentSong = queue.Any() ? (Song?)queue.DequeueSong() : null;
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
                        MediaSources = queue.Any() ? queue.DequeueStrings() : null;
                        break;

                    case nameof(PlayState):
                        PlayState = queue.DequeuePlayState();
                        break;

                    case nameof(Position):
                        Position = queue.DequeueTimeSpan();
                        break;

                    case nameof(SearchKey):
                        SearchKey = queue.Any() ? queue.DequeueString() : null;
                        break;

                    case nameof(Volume):
                        Volume = queue.DequeueFloat();
                        break;

                    default:
                        context.AcceptPublish = false;
                        break;
                }
            }
            catch (Exception e)
            {
                context.AcceptPublish = false;

                try
                {
                    MqttApplicationMessage message = new MqttApplicationMessage()
                    {
                        Topic = "Debug",
                        Payload = Encoding.UTF8.GetBytes(Utils.GetTypeMessageAndStack(e)),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = true
                    };

                    await server.PublishAsync(message);
                }
                catch { }
            }

            messageInterceptingTopics.Remove(context.ApplicationMessage.Topic);
        }

        private void Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (messageInterceptingTopics != null && messageInterceptingTopics.Contains(topic)) return;

            MqttApplicationMessage message = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            };

            try
            {
                server.PublishAsync(message);
            }
            catch { }
        }

        protected override void OnAllSongsShuffledChanged()
        {
            base.OnAllSongsShuffledChanged();

            PublishAllSongsShuffled();
        }

        private void PublishAllSongsShuffled()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(AllSongsShuffled);

            Publish(nameof(AllSongsShuffled), queue);
        }

        protected override void OnAudioDataChanged()
        {
            base.OnAudioDataChanged();

            PublishAudioData();
        }

        private void PublishAudioData()
        {
            Publish(nameof(AudioData), AudioData, MqttQualityOfServiceLevel.AtMostOnce);
        }

        protected override void OnCurrentSongChanged()
        {
            base.OnCurrentSongChanged();

            PublishCurrentSong();
        }

        private void PublishCurrentSong()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentSong.HasValue) queue.Enqueue(CurrentSong.Value);

            Publish(nameof(CurrentSong), queue);
        }

        protected override void OnDurationChanged()
        {
            base.OnDurationChanged();

            PublishDuration();
        }

        private void PublishDuration()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Duration);

            Publish(nameof(Duration), queue);
        }

        protected override void OnFormatChanged()
        {
            base.OnFormatChanged();

            PublishFormat();
        }

        private void PublishFormat()
        {
            ByteQueue queue = new ByteQueue();
            if (Format != null) queue.Enqueue(Format);

            Publish(nameof(Format), queue, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        protected override void OnIsAllShuffleChanged()
        {
            base.OnIsAllShuffleChanged();

            PublishIsAllShuffle();
        }

        private void PublishIsAllShuffle()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsAllShuffle);

            Publish(nameof(IsAllShuffle), queue);
        }

        protected override void OnIsOnlySearchChanged()
        {
            base.OnIsOnlySearchChanged();

            PublishIsOnlySearch();
        }

        private void PublishIsOnlySearch()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsOnlySearch);

            Publish(nameof(IsOnlySearch), queue);
        }

        protected override void OnIsSearchShuffleChanged()
        {
            base.OnIsSearchShuffleChanged();

            PublishIsSearchShuffle();
        }

        private void PublishIsSearchShuffle()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsSearchShuffle);

            Publish(nameof(IsSearchShuffle), queue);
        }

        protected override void OnMediaSourcesChanged()
        {
            base.OnMediaSourcesChanged();

            PublishMediaSources();
        }

        private void PublishMediaSources()
        {
            ByteQueue queue = new ByteQueue();
            if (MediaSources != null) queue.Enqueue(MediaSources);

            Publish(nameof(MediaSources), queue);
        }

        protected override void OnPlayStateChanged()
        {
            base.OnPlayStateChanged();

            PublishPlayState();
        }

        private void PublishPlayState()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(PlayState);

            Publish(nameof(PlayState), queue);
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();

            PublishPosition();
        }

        private void PublishPosition()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Position);

            Publish(nameof(Position), queue);
        }

        protected override void OnSearchKeyChanged()
        {
            base.OnSearchKeyChanged();

            PublishSearchKey();
        }

        private void PublishSearchKey()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(SearchKey);

            Publish(nameof(SearchKey), queue);
        }

        protected override void OnServiceVolumeChanged()
        {
            base.OnServiceVolumeChanged();

            PublishServiceVolume();
        }

        private void PublishServiceVolume()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Volume);

            Publish(nameof(Volume), queue);
        }

        protected override IWaveProvider ToWaveProvider(IWaveProvider waveProvider)
        {
            if (this.waveProvider != null) this.waveProvider.ReadEvent -= WaveProvider_Read;

            Format = waveProvider.WaveFormat;

            this.waveProvider = new ReadEventWaveProvider(waveProvider);
            this.waveProvider.ReadEvent += WaveProvider_Read;

            return this.waveProvider;
        }

        private void WaveProvider_Read(object sender, WaveProviderReadEventArgs e)
        {
            Task.Factory.StartNew(() => AudioData = e.Buffer.Skip(e.Offset).Take(e.ReturnCount).ToArray());
        }

        public async override void Dispose()
        {
            base.Dispose();

            if (IsOpen) await CloseAsync();
        }
    }
}
