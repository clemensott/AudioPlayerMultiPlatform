using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public interface IPlayer
    {
        PlaybackState PlayState { get; set; }

        Song Source { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; set; }

        float Volume { get; set; }
    }
}
