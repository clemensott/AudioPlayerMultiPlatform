using Commen = AudioPlayerBackend.Common;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;
using MQTTnet;

namespace AudioPlayerFrontend.Join
{
    class MqttClient : Commen.IMqttClient
    {
        private IMqttClient parent;

        public event EventHandler<Commen.MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        public event EventHandler<Commen.MqttClientConnectedEventArgs> Connected;
        public event EventHandler<Commen.MqttClientDisconnectedEventArgs> Disconnected;

        public bool IsConnected { get { return parent.IsConnected; } }

        public MqttClient(IMqttClient parent)
        {
            this.parent = parent;

            parent.ApplicationMessageReceived += Parent_ApplicationMessageReceived;
            parent.Connected += Parent_Connected;
            parent.Disconnected += Parent_Disconnected;
        }

        private void Parent_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            ApplicationMessageReceived?.Invoke(this, e.ToBackend());
        }

        private void Parent_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            Connected?.Invoke(this, e.ToBackend());
        }

        private void Parent_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            Disconnected?.Invoke(this, e.ToBackend());
        }

        public async Task ConnectAsync(string serverAddress, int? port)
        {
            IMqttClientOptions options = new MqttClientOptionsBuilder()
                .WithTcpServer(serverAddress, port)
                .WithCommunicationTimeout(TimeSpan.FromSeconds(1))
                .Build();

            await parent.ConnectAsync(options);
        }

        public async Task DisconnectAsync()
        {
            await parent.DisconnectAsync();
        }

        public async Task PublishAsync(Commen.MqttApplicationMessage message)
        {
            await parent.PublishAsync(message.ToFrontend());
        }

        public async Task SubscribeAsync(string topic, Commen.MqttQualityOfServiceLevel qos)
        {
            await parent.SubscribeAsync(topic, qos.ToFrontend());
        }

        public async Task UnsubscribeAsync(string topic)
        {
            await parent.UnsubscribeAsync(topic);
        }
    }
}
