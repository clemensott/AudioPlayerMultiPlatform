using AudioPlayerBackend.Common;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace AudioPlayerFrontendWpf.Join
{
    class MqttClient : AudioPlayerBackend.Common.IMqttClient
    {
        private MQTTnet.Client.IMqttClient parent;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected { get { return parent.IsConnected; } }

        public MqttClient(MQTTnet.Client.IMqttClient parent)
        {
            this.parent = parent;
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
