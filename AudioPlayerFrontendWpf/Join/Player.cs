using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerFrontend.Join
{
    class Player : IWaveProviderPlayer
    {
        private bool stop, stopped;
        private NAudio.Wave.IWaveProvider waveProvider, nextWaveProvider;
        private readonly NAudio.Wave.WaveOut waveOut;
        private PlaybackState playState;

        public PlaybackState PlayState
        {
            get => playState;
            set
            {
                playState = value;
                ExecutePlayState();
            }
        }

        public float Volume { get => waveOut.Volume; set => waveOut.Volume = value; }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public Player(int deviceNumber = -1, IntPtr? windowHandle = null)
        {
            stop = false;
            stopped = true;

            waveOut = windowHandle.HasValue ? new NAudio.Wave.WaveOut(windowHandle.Value) : new NAudio.Wave.WaveOut();
            waveOut.DeviceNumber = deviceNumber;
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            stopped = true;

            if (nextWaveProvider != null)
            {
                if (waveProvider is IDisposable disposable) disposable.Dispose();

                waveProvider = nextWaveProvider;
                nextWaveProvider = null;

                waveOut.Init(waveProvider);
                ExecutePlayState();
            }
            else if (stop)
            {
                if (waveProvider is IDisposable disposable) disposable.Dispose();

                ExecutePlayState();

                stop = false;
            }
            else PlaybackStopped?.Invoke(this, e.ToBackend());
        }

        public void Play(Func<AudioPlayerBackend.Player.IWaveProvider> waveProviderFunc)
        {
            NAudio.Wave.IWaveProvider wp = GetInternalWaveProvider(waveProviderFunc());

            if (stopped)
            {
                waveProvider = wp;
                waveOut.Init(wp);
                ExecutePlayState();
            }
            else
            {
                nextWaveProvider = wp;
                waveOut.Stop();
            }
        }

        private static NAudio.Wave.IWaveProvider GetInternalWaveProvider(AudioPlayerBackend.Player.IWaveProvider baseWP)
        {
            switch (baseWP)
            {
                case NAudio.Wave.IWaveProvider iwp:
                    return iwp;

                default:
                    return new WaveProvider(baseWP);
            }
        }

        public void Stop()
        {
            if (!stopped)
            {
                stop = true;
                waveOut.Stop();
            }
            else if (waveProvider is IDisposable disposable) disposable.Dispose();
        }

        public void ExecutePlayState()
        {
            if (stop || waveProvider == null) return;

            switch (PlayState)
            {
                case PlaybackState.Stopped:
                    waveOut.Stop();
                    break;

                case PlaybackState.Playing:
                    stopped = false;
                    waveOut.Play();
                    break;

                case PlaybackState.Paused:
                    stopped = false;
                    waveOut.Pause();
                    break;
            }
        }

        public void Dispose()
        {
            if (waveProvider is IDisposable disposable) disposable.Dispose();

            waveOut.Dispose();
        }
    }
}
