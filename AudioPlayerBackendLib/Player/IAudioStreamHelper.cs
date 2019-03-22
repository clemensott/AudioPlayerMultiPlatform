using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public interface IAudioStreamHelper
    {
        IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format, IAudioServiceBase service);
    }
}
