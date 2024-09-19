using System;
using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public interface IPlayerService : IAudioService
    {
        IPlayer Player { get; }
    }
}
