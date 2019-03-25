using AudioPlayerBackend.Audio;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using StdOttStandard;
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

        private bool isStreaming, isOpening;
        private List<string> initProps;
        private readonly PublishQueue publishQueue = new PublishQueue();
        private readonly Queue<MqttApplicationMessage> receiveMessages = new Queue<MqttApplicationMessage>();
        private MqttApplicationMessage currentPublish;

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

        public string ServerAddress { get; private set; }

        public int? Port { get; private set; }

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

        public override bool IsOpen => MqttClient?.IsConnected ?? false;

        public IMqttClient MqttClient { get; }

        private MqttClientCommunicator(IAudioServiceBase service) : base(service)
        {
            MqttClient = new MqttFactory().CreateMqttClient();
            MqttClient.ApplicationMessageReceived += Client_ApplicationMessageReceived;
        }

        public MqttClientCommunicator(IAudioServiceBase service, string server, int? port = null) : this(service)
        {
            ServerAddress = server;
            Port = port;
        }

        public override async Task OpenAsync()
        {
            try
            {
                IsOpening = true;

                TopicFilter[] serviceTopics = GetTopicFilters().Concat(GetTopicFilters(Service.SourcePlaylist)).ToArray();
                initProps = serviceTopics.Select(t => t.Topic).ToList();

                IMqttClientOptions options = new MqttClientOptionsBuilder()
                    .WithTcpServer(ServerAddress, Port)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(1))
                    .Build();

                await MqttClient.ConnectAsync(options);

                Task.Run(ProcessPublish);
                ProcessReceive();

                await MqttClient.SubscribeAsync(serviceTopics);
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

                IsOpening = false;
            }
        }

        public override async Task CloseAsync()
        {
            await MqttClient.UnsubscribeAsync(nameof(Service.AudioFormat));
            await MqttClient.UnsubscribeAsync(nameof(Service.AudioData));

            await Task.WhenAll(GetTopicFilters().Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));
            await Task.WhenAll(GetTopicFilters(Service.SourcePlaylist).Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));
            await Task.WhenAll(Service.Playlists.SelectMany(GetTopicFilters).Select(tf => MqttClient.UnsubscribeAsync(tf.Topic)));

            await MqttClient.DisconnectAsync();
        }

        private IEnumerable<TopicFilter> GetTopicFilters()
        {
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return new TopicFilter(nameof(Service.Playlists), qos);
            yield return new TopicFilter(nameof(Service.CurrentPlaylist), qos);
            yield return new TopicFilter(nameof(Service.PlayState), qos);
            yield return new TopicFilter(nameof(Service.Volume), qos);
        }

        private IEnumerable<TopicFilter> GetTopicFilters(ISourcePlaylistBase playlist)
        {
            string id = playlist.ID + ".";
            const MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return new TopicFilter(id + nameof(playlist.CurrentSong), qos);
            yield return new TopicFilter(id + nameof(playlist.Songs), qos);
            yield return new TopicFilter(id + nameof(playlist.Duration), qos);
            yield return new TopicFilter(id + nameof(playlist.FileMediaSources), qos);
            yield return new TopicFilter(id + nameof(playlist.IsAllShuffle), qos);
            yield return new TopicFilter(id + nameof(playlist.IsSearchShuffle), qos);
            yield return new TopicFilter(id + nameof(playlist.Loop), qos);
            yield return new TopicFilter(id + nameof(playlist.Position), qos);
            yield return new TopicFilter(id + nameof(playlist.SearchKey), qos);
        }

        private IEnumerable<TopicFilter> GetTopicFilters(IPlaylistBase playlist)
        {
            string id = playlist.ID + ".";
            const MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce;

            yield return new TopicFilter(id + nameof(playlist.CurrentSong), qos);
            yield return new TopicFilter(id + nameof(playlist.Songs), qos);
            yield return new TopicFilter(id + nameof(playlist.Duration), qos);
            yield return new TopicFilter(id + nameof(playlist.IsAllShuffle), qos);
            yield return new TopicFilter(id + nameof(playlist.Loop), qos);
            yield return new TopicFilter(id + nameof(playlist.Position), qos);
        }

        private async Task ProcessPublish()
        {
            while (IsOpen)
            {
                try
                {
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish1");
                    ////if (!MqttClient.IsConnected)  await OpenAsync();

                    //DateTime time = DateTime.Now;
                    //while (time + TimeSpan.FromMilliseconds(100) > DateTime.Now) ;

                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish2");
                    currentPublish = publishQueue.Dequeue();

                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish3");
                    //time = DateTime.Now;
                    //while (time + TimeSpan.FromMilliseconds(100) > DateTime.Now) ;

                    Task waitForReply = Utils.WaitAsync(currentPublish);
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish4");
                    //Task waitForTimeOut = Task.Delay(timeout);
                    System.Diagnostics.Debug.WriteLine("ConsumerPublish5: " + currentPublish.Topic);

                    await MqttClient.PublishAsync(currentPublish);
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish6");

                    await Task.WhenAny(waitForReply, Task.Delay(timeout));
                    //System.Diagnostics.Debug.WriteLine("ConsumerPublish7");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("ConsumerPublishException:\r\n" + e);
                }

                //System.Diagnostics.Debug.WriteLine("ConsumerPublish8");
                MqttApplicationMessage message = currentPublish;
                //System.Diagnostics.Debug.WriteLine("ConsumerPublish9: " + (message == null));

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

            System.Diagnostics.Debug.WriteLine("Receive: " + rawTopic);
            if (currentPublish?.Topic == rawTopic && currentPublish.Payload.SequenceEqual(payload))
            {
                lock (currentPublish)
                {
                    Monitor.PulseAll(currentPublish);
                    currentPublish = null;

                    return;
                }
            }

            lock (receiveMessages)
            {
                receiveMessages.Enqueue(e.ApplicationMessage);

                Monitor.Pulse(receiveMessages);
            }
        }

        private async Task ProcessReceive()
        {
            while (IsOpen)
            {
                try
                {
                    await Utils.WaitAsync(receiveMessages, () => receiveMessages.Count == 0);
                    await ProcessApplicationMessage(receiveMessages.Dequeue());
                }
                catch { }
            }
        }

        private async Task ProcessApplicationMessage(MqttApplicationMessage e)
        {
            string rawTopic = e.Topic;
            byte[] payload = e.Payload;
            System.Diagnostics.Debug.WriteLine("Process: " + rawTopic);

            LockTopic(rawTopic, payload);

            try
            {
                HandleMessage(rawTopic, payload);
            }
            catch (Exception exc)
            {
                string text = "rawTopic: " + rawTopic + "\r\n" + exc;
                System.Diagnostics.Debug.WriteLine(text);

                await PublishDebug(text);
            }

            if (initProps != null && initProps.Contains(rawTopic))
            {
                lock (initProps)
                {
                    initProps.Remove(rawTopic);

                    Monitor.Pulse(initProps);
                }
            }

            UnlockTopic(rawTopic);
        }

        protected override async Task SubscribeOrPublishAsync(IPlaylistBase playlist)
        {
            System.Diagnostics.Debug.WriteLine("SubscribeOrPublishAsync: " + playlist.ID);
            await MqttClient.SubscribeAsync(GetTopicFilters(playlist));
        }

        protected override async Task UnsubscribeOrUnpublishAsync(IPlaylistBase playlist)
        {
            System.Diagnostics.Debug.WriteLine("UnsubscribeOrUnpublishAsync: " + playlist.ID);
            await MqttClient.UnsubscribeAsync(GetTopicFilters(playlist).Select(t => t.Topic));
        }

        protected override async Task PublishAsync(MqttApplicationMessage message)
        {
            if (!IsOpen || IsTopicLocked(message.Topic, message.Payload)) return;

            publishQueue.Enqueue(message);

            await Utils.WaitAsync(message);
        }

        public override async void Dispose()
        {
            if (IsOpen) await CloseAsync();
        }
    }
}
