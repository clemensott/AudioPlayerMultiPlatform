using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Common;

namespace AudioPlayerBackend.Common
{
    public interface IMqttClient
    {
        bool IsConnected { get; }

        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos);
        Task UnsubscribeAsync(string topic);
        Task DisconnectAsync();
        Task ConnectAsync(string serverAddress, int? port);
        Task PublishAsync(MqttApplicationMessage message);
    }
}
