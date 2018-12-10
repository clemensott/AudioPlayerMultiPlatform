using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class Players : IPlayer
    {
        private readonly List<IPlayer> players;
        private PlaybackState playState;
        private float volume;

        public PlaybackState PlayState
        {
            get { return playState; }
            set
            {
                playState = value;
                ExecutePlayState();
            }
        }

        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;

                foreach (IPlayer player in players) player.Volume = value;
            }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public Players()
        {
            players = new List<IPlayer>();
            Volume = 1;
        }

        public void Play(Func<AudioPlayerBackend.Common.IWaveProvider> waveProviderFunc)
        {
            foreach (IPlayer player in players) player.Play(waveProviderFunc);
        }

        public void Stop(IDisposable dispose)
        {
            foreach (IPlayer player in players) player.Stop(dispose);
        }

        public void AddPlayer(IPlayer player)
        {
            player.PlaybackStopped += Player_PlaybackStopped;

            player.PlayState = PlayState;
            player.Volume = volume;

            players.Add(player);
        }

        public void RemovePlayer(IPlayer player)
        {
            player.PlaybackStopped -= Player_PlaybackStopped;

            players.Remove(player);
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, e);
        }

        public void ExecutePlayState()
        {
            foreach (IPlayer player in players) player.PlayState = PlayState;
        }
    }
}
