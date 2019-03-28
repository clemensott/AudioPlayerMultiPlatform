﻿using AudioPlayerBackend.Audio;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.MQTT
{
    public class MqttServerCommunicator : MqttCommunicator
    {
        private bool isOpen, isOpening;
        private readonly IMqttServer server;

        public int Port { get; private set; }

        public bool IsOpening
        {
            get => isOpening;
            private set
            {
                if (value == isOpening) return;

                isOpening = value;
                OnPropertyChanged(nameof(IsOpening));
            }
        }

        public override bool IsOpen => isOpen;

        public MqttServerCommunicator(IAudioServiceBase service, int port) : base(service)
        {
            server = new MqttFactory().CreateMqttServer();

            Port = port;
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
        {
            try
            {
                IsOpening = true;
                isOpen = true;

                IMqttServerOptions options = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(Port)
                    .WithApplicationMessageInterceptor(OnApplicationMessageInterception)
                    .Build();

                if (statusToken?.IsEnded.HasValue == true) return;
                await server.StartAsync(options);

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
                IsOpening = false;
            }
        }

        public override async Task CloseAsync()
        {
            isOpen = false;

            await server.StopAsync();
        }

        private async void OnApplicationMessageInterception(MqttApplicationMessageInterceptorContext context)
        {
            if (context.ClientId == null) return;

            string rawTopic = context.ApplicationMessage.Topic;
            byte[] payload = context.ApplicationMessage.Payload;

            LockTopic(rawTopic, payload);

            System.Diagnostics.Debug.WriteLine("rawTopic1: " + rawTopic);

            try
            {
                HandleMessage(rawTopic, payload);
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

        public async override void Dispose()
        {
            if (IsOpen) await CloseAsync();
        }
    }
}
