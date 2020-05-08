using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.MQTT
{
    public class MqttServerCommunicator : MqttCommunicator, IServerCommunicator, IMqttServerApplicationMessageInterceptor
    {
        private bool isOpen;
        private readonly IMqttServer server;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public int Port { get; }

        public override bool IsOpen => isOpen;

        public override string Name => "MQTT Server: " + Port;

        public MqttServerCommunicator(int port, INotifyPropertyChangedHelper helper = null) : base(helper)
        {
            server = new MqttFactory().CreateMqttServer();

            Port = port;
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
        {
            try
            {
                IsSyncing = true;

                IMqttServerOptions options = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(Port)
                    .WithApplicationMessageInterceptor(this)
                    .Build();

                if (statusToken?.IsEnded.HasValue == true) return;

                isOpen = true;
                await server.StartAsync(options);
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
                IsSyncing = false;
            }
        }

        public override async Task SetService(IAudioServiceBase service, BuildStatusToken statusToken)
        {
            try
            {
                IsSyncing = true;

                Unsubscribe(Service);
                Service = service;
                InitPlaylists();

                await SyncService(statusToken, false);
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public override async Task SyncService(BuildStatusToken statusToken)
        {
            await SyncService(statusToken, true);
        }

        private async Task SyncService(BuildStatusToken statusToken, bool unsubscribe)
        {
            try
            {
                IsSyncing = true;

                if (unsubscribe) Unsubscribe(Service);
                Subscribe(Service);

                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishCurrentPlaylist();
                if (statusToken?.IsEnded.HasValue == true) return;
                await Task.WhenAll(Service.Playlists.Select(PublishPlaylist));
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishPlaylists();
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishPlayState();
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishVolume();
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishFormat();
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishAudioData();

                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishPlaylist(Service.SourcePlaylist);
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishIsSearchShuffle(Service.SourcePlaylist);
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishSearchKey(Service.SourcePlaylist);
                if (statusToken?.IsEnded.HasValue == true) return;
                await PublishMediaSources(Service.SourcePlaylist);
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public override async Task CloseAsync()
        {
            isOpen = false;

            await server.StopAsync();
        }

        public async Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            if (context.ClientId == null) return;

            string rawTopic = context.ApplicationMessage.Topic;
            byte[] payload = context.ApplicationMessage.Payload;

            LockTopic(rawTopic, payload);

            System.Diagnostics.Debug.WriteLine("rawTopic1: " + rawTopic);

            try
            {
                context.AcceptPublish = await HandleMessage(rawTopic, payload);
            }
            catch (Exception e)
            {
                context.AcceptPublish = false;

                System.Diagnostics.Debug.WriteLine(e.ToString());
                await PublishDebug(e);
            }

            System.Diagnostics.Debug.WriteLine("rawTopic2: " + rawTopic);

            UnlockTopic(rawTopic);
        }

        protected override Task SubscribeAsync(IPlaylistBase playlist)
        {
            System.Diagnostics.Debug.WriteLine("SubscribeOrPublishAsync: " + playlist.ID);
            return Task.CompletedTask;
            //await Task.WhenAll(Service.Playlists.Select(PublishPlaylist).ToArray());
        }

        protected override async Task PublishAsync(MqttApplicationMessage message)
        {
            if (!IsOpen || IsTopicLocked(message.Topic, message.Payload)) return;

            try
            {
                await server.PublishAsync(message);
            }
            catch (Exception e)
            {
                await PublishDebug(e);
            }
        }

        public override async void Dispose()
        {
            if (IsOpen) await CloseAsync();
        }
    }
}
