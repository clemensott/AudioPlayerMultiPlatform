﻿using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Common
{
    public interface IMqttClient
    {
        bool IsConnected { get; }

        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos);
        Task UnsubscribeAsync(string topic);
        Task DisconnectAsync();
        Task ConnectAsync(string serverAddress, int? port);
        Task PublishAsync(MqttApplicationMessage message);
    }
}
