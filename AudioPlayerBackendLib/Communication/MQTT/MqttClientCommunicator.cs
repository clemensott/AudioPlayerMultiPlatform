using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using StdOttStandard;
using StdOttStandard.Linq.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.MQTT
{
    public class MqttClientCommunicator : MqttCommunicator
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        private bool isDisconnecting, raiseDisconnected, isStreaming, isReceiveProcessorRunning;
        private InitList<string> initProps;
        private readonly PublishQueue publishQueue = new PublishQueue();
        private readonly AsyncQueue<MqttApplicationMessage> receiveMessages = new AsyncQueue<MqttApplicationMessage>();
        private MqttApplicationMessage currentPublish;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public bool IsStreaming
        {
            get => isStreaming;
            set
            {
                if (value == isStreaming) return;

                isStreaming = value;

                if (value)
                {
                    MqttClient.SubscribeAsync(nameof(Service.AudioFormat), MqttQualityOfServiceLevel.AtLeastOnce);
                    MqttClient.SubscribeAsync(nameof(Service.AudioData), MqttQualityOfServiceLevel.AtMostOnce);
                }
                else
                {
                    MqttClient.UnsubscribeAsync(nameof(Service.AudioData));
                    MqttClient.UnsubscribeAsync(nameof(Service.AudioFormat));
                }

                OnPropertyChanged(nameof(IsStreaming));
            }
        }

        public string ServerAddress { get; }

        public int? Port { get; }

        public override bool IsOpen => MqttClient?.IsConnected ?? false;

        public IMqttClient MqttClient { get; }

        public InitList<string> InitProps
        {
            get => initProps;
            private set
            {
                if (value == initProps) return;

                initProps = value;
                OnPropertyChanged(nameof(InitProps));
            }
        }

        public override string Name => string.Format("{0}:{1}", ServerAddress.Trim(), Port?.ToString() ?? "Default");

        public MqttClientCommunicator(string server, int? port = null,
            INotifyPropertyChangedHelper helper = null) : base(helper)
        {
            MqttClient = new MqttFactory().CreateMqttClient();
            MqttClient.UseApplicationMessageReceivedHandler(OnApplicationMessageReceived);
            MqttClient.UseDisconnectedHandler((Action<MqttClientDisconnectedEventArgs>)OnDisconnected);

            ServerAddress = server;
            Port = port;
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
        {
            try
            {
                IMqttClientOptions options = new MqttClientOptionsBuilder()
                    .WithTcpServer(ServerAddress, Port)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(1))
                    .Build();

                if (statusToken?.IsEnded.HasValue == true) return;
                raiseDisconnected = false;
                await MqttClient.ConnectAsync(options);
                if (statusToken?.IsEnded.HasValue == true) return;

                Task.Run(ProcessPublish);
                ProcessReceive();
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
                try
                {
                    if (statusToken?.IsEnded.HasValue == true) await CloseAsync();
                }
                catch { }

                InitProps = null;
                IsSyncing = false;
            }
        }

        public override async Task SetService(IAudioServiceBase service, BuildStatusToken statusToken)
        {
            try
            {
                await UnsubscribeTopics();

                Unsubscribe(Service);
                Service = service;

                await SyncService(statusToken, false);
            }
            catch
            {
                await UnsubscribeTopics();

                throw;
            }
            finally
            {
                InitProps = null;
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

                if (unsubscribe)
                {
                    await UnsubscribeTopics();
                    Unsubscribe(Service);
                    playlists.Clear();
                }

                Subscribe(Service);

                TopicFilter[] serviceTopics = GetTopicFilters().Concat(GetTopicFilters(Service.SourcePlaylist)).ToArray();
                InitProps = new InitList<string>(serviceTopics.Select(t => t.Topic));
                System.Diagnostics.Debug.WriteLine("Initlist: " + unsubscribe);

                await MqttClient.SubscribeAsync(serviceTopics);

                if (statusToken != null)
                {
                    if (statusToken.IsEnded.HasValue) return;

                    await Task.WhenAny(InitProps.Task, statusToken.EndTask);
                }
                else await InitProps.Task;
            }
            catch
            {
                await UnsubscribeTopics();

                throw;
            }
            finally
            {
                InitProps = null;
                IsSyncing = false;
            }
        }

        public override async Task CloseAsync()
        {
            try
            {
                isDisconnecting = true;
                await UnsubscribeTopics();
                await MqttClient.DisconnectAsync();
            }
            finally
            {
                isDisconnecting = false;
            }
        }

        private async Task UnsubscribeTopics()
        {
            if (Service == null || !IsOpen) return;

            await MqttClient.UnsubscribeAsync(nameof(Service.AudioFormat));
            await MqttClient.UnsubscribeAsync(nameof(Service.AudioData));

            await Task.WhenAll(GetTopicFilters().Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));
            await Task.WhenAll(GetTopicFilters(Service.SourcePlaylist).Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));
            await Task.WhenAll(GetTopicFilters(playlists.Values).Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));
        }

        private IEnumerable<TopicFilter> GetTopicFilters()
        {
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return GetTopicFilter(nameof(Service.Playlists), qos);
            yield return GetTopicFilter(nameof(Service.CurrentPlaylist), qos);
            yield return GetTopicFilter(nameof(Service.PlayState), qos);
            yield return GetTopicFilter(nameof(Service.Volume), qos);
        }

        private static IEnumerable<TopicFilter> GetTopicFilters(ISourcePlaylistBase playlist)
        {
            if (playlist == null) yield break;

            string id = playlist.ID + ".";
            const MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return GetTopicFilter(id + nameof(playlist.CurrentSong), qos);
            yield return GetTopicFilter(id + nameof(playlist.Songs), qos);
            yield return GetTopicFilter(id + nameof(playlist.Duration), qos);
            yield return GetTopicFilter(id + nameof(playlist.FileMediaSources), qos);
            yield return GetTopicFilter(id + nameof(playlist.IsAllShuffle), qos);
            //yield return GetTopicFilter(id + nameof(playlist.IsSearchShuffle), qos);
            yield return GetTopicFilter(id + nameof(playlist.Loop), qos);
            yield return GetTopicFilter(id + nameof(playlist.Position), qos);
            //yield return GetTopicFilter(id + nameof(playlist.SearchKey), qos);
        }

        private static IEnumerable<TopicFilter> GetTopicFilters(IEnumerable<IPlaylistBase> playlists)
        {
            return playlists.SelectMany(GetTopicFilters);
        }

        private static IEnumerable<TopicFilter> GetTopicFilters(IPlaylistBase playlist)
        {
            const MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            return GetTopics(playlist).Select(t => GetTopicFilter(t, qos));
        }

        private static TopicFilter GetTopicFilter(string topic, MqttQualityOfServiceLevel qos)
        {
            return new TopicFilter()
            {
                Topic = topic,
                QualityOfServiceLevel = qos,
            };
        }

        private async Task ProcessPublish()
        {
            while (IsOpen)
            {
                MqttApplicationMessage message;
                try
                {
                    currentPublish = publishQueue.Dequeue();

                    if (currentPublish.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        Task waitForReply = StdUtils.WaitAsync(currentPublish);
                        await MqttClient.PublishAsync(currentPublish);

                        await Task.WhenAny(waitForReply, Task.Delay(timeout));
                    }
                    else
                    {
                        await MqttClient.PublishAsync(currentPublish);

                        message = currentPublish;
                        if (message != null)
                        {
                            lock (currentPublish) Monitor.PulseAll(currentPublish);
                        }

                        continue;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"ConsumerPublishException: {currentPublish?.Topic} {IsOpen}\r\n{e}");
                    await CloseAsync();
                    return;
                }

                message = currentPublish;
                if (message == null) continue;

                lock (message)
                {
                    currentPublish = null;

                    if (!publishQueue.IsEnqueued(message.Topic))
                    {
                        publishQueue.Enqueue(message);
                    }
                    else
                    {
                        Monitor.PulseAll(message);
                    }
                }
            }
        }

        private async Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string rawTopic = e.ApplicationMessage.Topic;
            byte[] payload = e.ApplicationMessage.Payload;

            if (currentPublish?.Topic == rawTopic && currentPublish.Payload.SequenceEqual(payload))
            {
                lock (currentPublish)
                {
                    Monitor.PulseAll(currentPublish);
                    currentPublish = null;

                    return;
                }
            }

            await receiveMessages.Enqueue(e.ApplicationMessage);
        }

        private async Task ProcessReceive()
        {
            if (isReceiveProcessorRunning) return;
            isReceiveProcessorRunning = true;

            while (IsOpen)
            {
                try
                {
                    (bool isEnd, MqttApplicationMessage item) = await receiveMessages.Dequeue();
                    if (isEnd) break;

                    await ProcessApplicationMessage(item);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("ProcessReceiveCatch: " + e);
                }
            }

            isReceiveProcessorRunning = false;
        }

        private async Task ProcessApplicationMessage(MqttApplicationMessage e)
        {
            string rawTopic = e.Topic;
            byte[] payload = e.Payload;
            System.Diagnostics.Debug.WriteLine("Process: " + rawTopic);
            LockTopic(rawTopic, payload);

            try
            {
                await HandleMessage(rawTopic, payload);
            }
            catch (Exception exc)
            {
                string text = "rawTopic: " + rawTopic + "\r\n" + exc;
                System.Diagnostics.Debug.WriteLine(text);

                await PublishDebug(text);
            }

            InitProps?.Remove(rawTopic);

            UnlockTopic(rawTopic);
        }

        private void OnDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            RaiseDisconnected(arg.Exception);
        }

        private void RaiseDisconnected(Exception exception)
        {
            //if (raiseDisconnected) return;
            raiseDisconnected = true;

            Disconnected?.Invoke(this, new DisconnectedEventArgs(isDisconnecting, exception));
        }

        protected override async Task SubscribeAsync(IPlaylistBase playlist)
        {
            InitProps?.AddRange(GetTopicFilters(playlist).Select(tf => tf.Topic));

            await MqttClient.SubscribeAsync(GetTopicFilters(playlist).ToArray());
        }

        protected override async Task PublishAsync(MqttApplicationMessage message)
        {
            if ((IsSyncing && message.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce) ||
                !IsOpen || IsTopicLocked(message.Topic, message.Payload)) return;

            publishQueue.Enqueue(message);
            await StdUtils.WaitAsync(message);
        }

        public override async void Dispose()
        {
            if (IsOpen) await CloseAsync();
        }
    }
}
