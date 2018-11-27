using System;

namespace AudioPlayerBackend.Common
{
    public interface IPositionWaveProvider : IWaveProvider
    {
        TimeSpan CurrentTime { get; set; }

        TimeSpan TotalTime { get; }
    }
}
