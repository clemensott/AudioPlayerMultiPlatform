using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public class MqttAudioService : AudioService, IMqttAudioService
    {
        private bool isOpenning;
        private readonly IMqttAudioServiceHelper helper;
        private readonly Dictionary<string, byte[]> interceptingDict = new Dictionary<string, byte[]>();
        private readonly IMqttServer server;
        private ReadEventWaveProvider waveProvider;

        public int Port { get; private set; }

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

        public bool IsOpen { get; private set; }

        public MqttAudioService(IPlayer player, int port, IMqttAudioServiceHelper helper = null) : base(player, helper)
        {
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
            try
            {
                IsOpenning = true;
                IsOpen = true;

                await server.StartAsync(Port, OnApplicationMessageInterception);

                await PublishMediaSources();
                await PublishAdditionalPlaylists();
                await PublishCurrentPlaylist();
                await PublishPlayState();
                await PublishServiceVolume();
                await PublishAllPlaylistProperties(FileBasePlaylist);

                await PublishCurrentPlaylist();
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
                IsOpenning = false;
            }
        }

        public async Task CloseAsync()
        {
            IsOpen = false;

            await server.StopAsync();
        }

        private async void OnApplicationMessageInterception(MqttApplicationMessageInterceptorContext context)
        {
            if (context.ClientId == null) return;

            string rawTopic = context.ApplicationMessage.Topic;
            byte[] payload = context.ApplicationMessage.Payload;

            MqttAudioUtils.LockTopic(interceptingDict, rawTopic, payload);

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
            catch (Exception e)
            {
                context.AcceptPublish = false;

                await PublishDebug(e);
            }

            MqttAudioUtils.UnlockTopic(interceptingDict, rawTopic);
        }

        private async Task PublishDebug(Exception e)
        {
            try
            {
                MqttApplicationMessage message = new MqttApplicationMessage()
                {
                    Topic = "Debug",
                    Payload = Encoding.UTF8.GetBytes(e.ToString()),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = true
                };

                await server.PublishAsync(message);
            }
            catch { }
        }

        private async Task PublishAllPlaylistProperties(IPlaylist p)
        {
            await PublishSongs(p);

            await Task.WhenAll(PublishCurrentSong(p), PublishDuration(p),
                PublishIsAllShuffle(p), PublishIsOnlySearch(p), PublishIsSearchShuffle(p),
                PublishPosition(p), PublishSearchKey(p), PublishLoop(p));
        }

        private async Task Publish(IPlaylist playlist, string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            await Publish(playlist.ID + "." + topic, payload, qos, retain);
        }

        private async Task Publish(string topic, byte[] payload,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = true)
        {
            if (!IsOpen || MqttAudioUtils.IsTopicLocked(interceptingDict, topic, payload)) return;

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

        protected async override void OnAddPlaylist(IPlaylist playlist)
        {
            base.OnAddPlaylist(playlist);

            await PublishAdditionalPlaylists();
        }

        protected async override void OnRemovePlaylist(IPlaylist playlist)
        {
            base.OnRemovePlaylist(playlist);

            await PublishAdditionalPlaylists();
        }

        private async Task PublishAdditionalPlaylists()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(AdditionalPlaylists);

            await Publish(nameof(AdditionalPlaylists), queue);
        }

        protected async override void OnCurrenPlaylistChanged()
        {
            base.OnCurrenPlaylistChanged();

            await PublishCurrentPlaylist();
        }

        private async Task PublishCurrentPlaylist()
        {
            ByteQueue queue = new ByteQueue();
            if (CurrentPlaylist != null) queue.EnqueueRange(CurrentPlaylist.ID.ToByteArray());

            await Publish(nameof(CurrentPlaylist), queue);
        }

        protected async override void OnSongsChanged(IPlaylist playlist)
        {
            base.OnSongsChanged(playlist);

            await PublishSongs(playlist);
        }

        private async Task PublishSongs(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Songs);

            await Publish(playlist, nameof(playlist.Songs), queue);
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

        protected async override void OnCurrentSongChanged(IPlaylist playlist)
        {
            base.OnCurrentSongChanged(playlist);

            await PublishCurrentSong(playlist);
        }

        private async Task PublishCurrentSong(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            if (playlist.CurrentSong.HasValue) queue.Enqueue(playlist.CurrentSong.Value);

            await Publish(playlist, nameof(playlist.CurrentSong), queue);
        }

        protected async override void OnDurationChanged(IPlaylist playlist)
        {
            base.OnDurationChanged(playlist);

            await PublishDuration(playlist);
        }

        private async Task PublishDuration(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Duration);

            await Publish(playlist, nameof(playlist.Duration), queue);
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

        protected async override void OnIsAllShuffleChanged(IPlaylist playlist)
        {
            base.OnIsAllShuffleChanged(playlist);

            await PublishIsAllShuffle(playlist);
        }

        private async Task PublishIsAllShuffle(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsAllShuffle);

            await Publish(playlist, nameof(playlist.IsAllShuffle), queue);
        }

        protected async override void OnIsOnlySearchChanged(IPlaylist playlist)
        {
            base.OnIsOnlySearchChanged(playlist);

            await PublishIsOnlySearch(playlist);
        }

        private async Task PublishIsOnlySearch(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsOnlySearch);

            await Publish(playlist, nameof(playlist.IsOnlySearch), queue);
        }

        protected override async void OnIsSearchShuffleChangedAsync(IPlaylist playlist)
        {
            base.OnIsSearchShuffleChangedAsync(playlist);

            await PublishIsSearchShuffle(playlist);
        }

        private async Task PublishIsSearchShuffle(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.IsSearchShuffle);

            await Publish(playlist, nameof(playlist.IsSearchShuffle), queue);
        }

        protected async override void OnMediaSourcesChanged()
        {
            base.OnMediaSourcesChanged();

            await PublishMediaSources();
        }

        private async Task PublishMediaSources()
        {
            ByteQueue queue = new ByteQueue();
            if (FileMediaSources != null) queue.Enqueue(FileMediaSources);

            await Publish(nameof(FileMediaSources), queue);
        }

        protected async override void OnPlayStateChanged()
        {
            base.OnPlayStateChanged();

            await PublishPlayState();
        }

        private async Task PublishPlayState()
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue((int)PlayState);

            await Publish(nameof(PlayState), queue);
        }

        protected async override void OnPositionChanged(IPlaylist playlist)
        {
            base.OnPositionChanged(playlist);

            await PublishPosition(playlist);
        }

        private async Task PublishPosition(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.Position);

            await Publish(playlist, nameof(playlist.Position), queue);
        }

        protected async override void OnLoopChanged(IPlaylist playlist)
        {
            base.OnLoopChanged(playlist);

            await PublishLoop(playlist);
        }

        private async Task PublishLoop(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue((int)playlist.Loop);

            await Publish(playlist, nameof(playlist.Loop), queue);
        }

        protected async override void OnSearchKeyChanged(IPlaylist playlist)
        {
            base.OnSearchKeyChanged(playlist);

            await PublishSearchKey(playlist);
        }

        private async Task PublishSearchKey(IPlaylist playlist)
        {
            ByteQueue queue = new ByteQueue();
            queue.Enqueue(playlist.SearchKey);

            await Publish(playlist, nameof(playlist.SearchKey), queue);
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
