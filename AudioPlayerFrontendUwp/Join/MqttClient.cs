using AudioPlayerBackend.Common;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class MqttClient : AudioPlayerBackend.Common.IMqttClient
    {
        private MQTTnet.Client.IMqttClient parent;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected { get { return parent.IsConnected; } }

        public MqttClient(MQTTnet.Client.IMqttClient parent)
        {
            this.parent = parent;
            parent.ApplicationMessageReceived += Parent_ApplicationMessageReceived;
            parent.Connected += Parent_Connected;
            parent.Disconnected += Parent_Disconnected;
        }

        private void Parent_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MqttClientConnected");
        }

        private void Parent_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MqttClientDisconnected");
        }

        private void Parent_ApplicationMessageReceived(object sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
        {
            ApplicationMessageReceived?.Invoke(this, e.ToBackend());
        }

        public async Task ConnectAsync(string serverAddress, int? port)
        {
            IMqttClientOptions options = new MqttClientOptionsBuilder()
                .WithTcpServer(serverAddress, port)
                .Build();

            await parent.ConnectAsync(options);
        }

        public async Task DisconnectAsync()
        {
            await parent.DisconnectAsync();
        }

        public async Task PublishAsync(MqttApplicationMessage message)
        {
            await parent.PublishAsync(message.ToFrontend());
        }

        public async Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos)
        {
            await parent.SubscribeAsync(topic, qos.ToFrontend());
        }

        public async Task UnsubscribeAsync(string topic)
        {
            await parent.UnsubscribeAsync(topic);
        }
    }
}
