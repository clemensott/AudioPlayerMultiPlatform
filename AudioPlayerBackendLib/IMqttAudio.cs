using NAudio.Wave;
using System.Threading.Tasks;

namespace AudioPlayerBackendLib
{
    public interface IMqttAudio : IAudioExtended
    {
        bool IsOpen { get; }

        WaveFormat Format { get; set; }

        byte[] AudioData { get; set; }

        Task OpenAsync();

        Task CloseAsync();
    }
}
