using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public class MqttAudioService : AudioService, IMqttAudioService
    {
        private readonly IMqttAudioServiceHelper helper;
        private readonly List<(string topic, byte[] payload)> interceptingTuples;
        private readonly IMqttServer server;
        private ReadEventWaveProvider waveProvider;

        public int Port { get; private set; }

        public bool IsOpen { get; private set; }

        public MqttAudioService(IPlayer player, int port, IMqttAudioServiceHelper helper = null) : base(player, helper)
        {
            interceptingTuples = new List<(string topic, byte[] payload)>();

            this.helper = helper;
            server = CreateMqttServer();

            Port = port;
        }

        protected virtual IMqttServer CreateMqttServer()
        {
            return helper.CreateMqttServer(this);
        }

        public async Task OpenAsync()
        {
            IsOpen = true;

            await server.StartAsync(Port, OnApplicationMessageInterception);

            await PublishAllSongsShuffled();
            await PublishCurrentSong();
            await PublishDuration();
            await PublishIsAllShuffle();
            await PublishIsOnlySearch();
            await PublishIsSearchShuffle();
            await PublishMediaSources();
            await PublishPlayState();
            await PublishPosition();
            await PublishSearchKey();
            await PublishServiceVolume();
        }

        public async Task CloseAsync()
        {
            IsOpen = false;

            await server.StopAsync();
        }

        private async void OnApplicationMessageInterception(MqttApplicationMessageInterceptorContext context)
        {
            if (context.ClientId == null) return;

            string topic = context.ApplicationMessage.Topic;
            byte[] payload = context.ApplicationMessage.Payload;

            interceptingTuples.Add((topic, payload));

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

                await PublishDebug(e);
            }

            interceptingTuples.Remove((topic, payload));
        }

        private async Task PublishDebug(Exception e)
        {
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

        private async Task Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (interceptingTuples?.Any(t => t.topic == topic && t.payload.SequenceEqual(payload)) ?? false) return;

            MqttApplicationMessage message = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = payload,
                QualityOfServiceLevel = qos,
                Retain = retain
            };

            try
            {
                await server.PublishAsync(message);
            }
            catch (Exception e)
            {
                await PublishDebug(e);
            }
        }

        protected async override void OnAllSongsShuffledChanged()
        {
            base.OnAllSongsShuffledChanged();

            await PublishAllSongsShuffled();
        }

        private async Task PublishAllSongsShuffled()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(AllSongsShuffled);

            await Publish(nameof(AllSongsShuffled), queue);
        }

        protected async override void OnAudioDataChanged()
        {
            base.OnAudioDataChanged();

            await PublishAudioData();
        }

        private async Task PublishAudioData()
        {
            await Publish(nameof(AudioData), AudioData, MqttQualityOfServiceLevel.AtMostOnce);
        }

        protected async override void OnCurrentSongChanged()
        {
            base.OnCurrentSongChanged();

            await PublishCurrentSong();
        }

        private async Task PublishCurrentSong()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentSong.HasValue) queue.Enqueue(CurrentSong.Value);

            await Publish(nameof(CurrentSong), queue);
        }

        protected async override void OnDurationChanged()
        {
            base.OnDurationChanged();

            await PublishDuration();
        }

        private async Task PublishDuration()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Duration);

            await Publish(nameof(Duration), queue);
        }

        protected async override void OnFormatChanged()
        {
            base.OnFormatChanged();

            await PublishFormat();
        }

        private async Task PublishFormat()
        {
            ByteQueue queue = new ByteQueue();
            if (Format != null) queue.Enqueue(Format);

            await Publish(nameof(Format), queue, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        protected async override void OnIsAllShuffleChanged()
        {
            base.OnIsAllShuffleChanged();

            await PublishIsAllShuffle();
        }

        private async Task PublishIsAllShuffle()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsAllShuffle);

            await Publish(nameof(IsAllShuffle), queue);
        }

        protected async override void OnIsOnlySearchChanged()
        {
            base.OnIsOnlySearchChanged();

            await PublishIsOnlySearch();
        }

        private async Task PublishIsOnlySearch()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsOnlySearch);

            await Publish(nameof(IsOnlySearch), queue);
        }

        protected async override void OnIsSearchShuffleChanged()
        {
            base.OnIsSearchShuffleChanged();

            await PublishIsSearchShuffle();
        }

        private async Task PublishIsSearchShuffle()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(IsSearchShuffle);

            await Publish(nameof(IsSearchShuffle), queue);
        }

        protected async override void OnMediaSourcesChanged()
        {
            base.OnMediaSourcesChanged();

            await PublishMediaSources();
        }

        private async Task PublishMediaSources()
        {
            ByteQueue queue = new ByteQueue();
            if (MediaSources != null) queue.Enqueue(MediaSources);

            await Publish(nameof(MediaSources), queue);
        }

        protected async override void OnPlayStateChanged()
        {
            base.OnPlayStateChanged();

            await PublishPlayState();
        }

        private async Task PublishPlayState()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(PlayState);

            await Publish(nameof(PlayState), queue);
        }

        protected async override void OnPositionChanged()
        {
            base.OnPositionChanged();

            await PublishPosition();
        }

        private async Task PublishPosition()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Position);

            await Publish(nameof(Position), queue);
        }

        protected async override void OnSearchKeyChanged()
        {
            base.OnSearchKeyChanged();

            await PublishSearchKey();
        }

        private async Task PublishSearchKey()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(SearchKey);

            await Publish(nameof(SearchKey), queue);
        }

        protected async override void OnServiceVolumeChanged()
        {
            base.OnServiceVolumeChanged();

            await PublishServiceVolume();
        }

        private async Task PublishServiceVolume()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(Volume);

            await Publish(nameof(Volume), queue);
        }

        internal override IPositionWaveProvider ToWaveProvider(IPositionWaveProvider waveProvider)
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
