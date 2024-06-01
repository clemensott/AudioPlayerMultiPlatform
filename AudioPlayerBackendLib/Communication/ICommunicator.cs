using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public interface ICommunicator : IAudioService
    {
        event EventHandler<DisconnectedEventArgs> Disconnected;

        bool IsOpen { get; }

        string Name { get; }

        IAudioServiceBase Service { get; }

        Task OpenAsync(BuildStatusToken statusToken);

        Task SendCommand(string cmd);

        Task SetService(IAudioServiceBase service, BuildStatusToken statusToken);

        Task SyncService(BuildStatusToken statusToken);

        Task CloseAsync();
    }
}
