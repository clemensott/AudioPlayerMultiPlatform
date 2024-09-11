using AudioPlayerBackend.Build;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public interface ICommunicator : IAudioService
    {
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<ReceivedEventArgs> Received;

        bool IsOpen { get; }

        string Name { get; }

        Task SendCommand(string cmd);

        Task<byte[]> SendAsync(string topic, byte[] payload);
    }
}
