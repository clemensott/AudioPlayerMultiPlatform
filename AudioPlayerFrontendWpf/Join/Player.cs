using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerFrontendWpf.Join
{
    class Player : IPlayer
    {
        private readonly List<WaveProvider> waveProviders;
        private readonly Queue<WaveProvider> playWaveProviders, stopWaveProviders;
        private NAudio.Wave.WaveOut waveOut;
        private PlaybackState playState;

        public PlaybackState PlayState
        {
            get { return playState; }
            set
            {
                if (value == playState) return;

                playState = value;
                ExecutePlayState();
            }
        }

        public float Volume { get { return waveOut.Volume; } set { waveOut.Volume = value; } }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public Player(int deviceNumber = -1, IntPtr? windowHandle = null)
        {
            waveProviders = new List<WaveProvider>();
            playWaveProviders = new Queue<WaveProvider>();
            stopWaveProviders = new Queue<WaveProvider>();

            waveOut = windowHandle.HasValue ? new NAudio.Wave.WaveOut(windowHandle.Value) : new NAudio.Wave.WaveOut();

            waveOut.DeviceNumber = deviceNumber;
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            lock (stopWaveProviders)
            {

                if (stopWaveProviders.Count == 0 && playWaveProviders.Count == 0)
                {
                    PlaybackStopped?.Invoke(this, e.ToBackend());
                    return;
                }

                while (stopWaveProviders.Count > 0) stopWaveProviders.Dequeue().Parent.Dispose();
                while (playWaveProviders.Count > 0) waveOut.Init(playWaveProviders.Dequeue());
            }
        }

        public void Play(IWaveProvider waveProvider)
        {
            WaveProvider wp = new WaveProvider(waveProvider);
            waveProviders.Add(wp);

            lock (stopWaveProviders)
            {
                if (stopWaveProviders.Count > 0) playWaveProviders.Enqueue(wp);
                else
                {
                    waveOut.Init(wp);
                    ExecutePlayState();
                }
            }
        }

        public void Stop(IWaveProvider waveProvider)
        {
            if (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped) waveProvider.Dispose();
            else if (waveProviders.Any(wp => wp.Parent == waveProvider))
            {
                stopWaveProviders.Enqueue(waveProviders.First(wp => wp.Parent == waveProvider));
                waveOut.Stop();
            }
        }

        private void ExecutePlayState()
        {
            if (stopWaveProviders.Count > 0) return;

            switch (PlayState)
            {
                case PlaybackState.Stopped:
                    waveOut.Stop();
                    break;

                case PlaybackState.Playing:
                    waveOut.Play();
                    break;

                case PlaybackState.Paused:
                    waveOut.Pause();
                    break;
            }
        }
    }
}
