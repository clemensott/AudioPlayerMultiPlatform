using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerFrontendWpf.Join
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

                foreach (IPlayer player in players) player.PlayState = value;
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
            Volume = 1;
        }

        public void Play(IWaveProvider waveProvider)
        {
            foreach (IPlayer player in players) player.Play(waveProvider);
        }

        public void Stop(IWaveProvider waveProvider)
        {
            foreach (IPlayer player in players) player.Stop(waveProvider);
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
    }
}
