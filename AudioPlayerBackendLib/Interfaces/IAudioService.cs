using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerBackend
{
    public interface IAudioService : IAudioExtended
    {
        IPositionWaveProvider Reader { get; set; }
    }
}
