using AudioPlayerBackend.Audio;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public interface ICommunicator : IDisposable
    {
        bool IsOpen { get; }

        IAudioServiceBase Service { get; }

        Task OpenAsync();

        Task CloseAsync();
    }
}
