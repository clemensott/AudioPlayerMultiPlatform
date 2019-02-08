using AudioPlayerBackend.Common;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public interface IMqttAudio : IAudioExtended
    {
        bool IsOpenning { get; }

        bool IsOpen { get; }

        WaveFormat Format { get; set; }

        byte[] AudioData { get; set; }

        Task OpenAsync();

        Task CloseAsync();
    }
}
