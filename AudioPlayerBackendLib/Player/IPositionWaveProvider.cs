using System;

namespace AudioPlayerBackend.Player
{
    public interface IPositionWaveProvider : IWaveProvider
    {
        TimeSpan CurrentTime { get; set; }

        TimeSpan TotalTime { get; }
    }
}
