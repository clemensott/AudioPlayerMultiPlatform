using AudioPlayerBackend.Player;
using NAudio.CoreAudioApi;
//using NAudio.Wave.WaveOutputs;
using System;

namespace AudioPlayerFrontend.Join
{
    class Player : IWaveProviderPlayer
    {
        private bool stop, stopped;
        private NAudio.Wave.IWaveProvider waveProvider;
        private Func<NAudio.Wave.IWaveProvider> playWaveProviderFunc;
        //private readonly WasapiOutRT waveOut;
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

        public float Volume { get => 1f; set { } }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public Player()
        {
            stop = false;
            stopped = true;

            //waveOut = new WasapiOutRT(AudioClientShareMode.Shared, 200);
            //waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            stopped = true;

            if (playWaveProviderFunc != null)
            {
                if (waveProvider is IDisposable disposable) disposable.Dispose();

                //waveOut.Init(playWaveProviderFunc);
                playWaveProviderFunc = null;

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
            NAudio.Wave.IWaveProvider wpf()
            {
                return waveProvider = GetInternalWaveProvider(waveProviderFunc());
            }

            if (stopped)
            {
                if (waveProvider is IDisposable disposable) disposable.Dispose();

                //waveOut.Init(wpf);
                ExecutePlayState();
            }
            else
            {
                playWaveProviderFunc = wpf;
                //waveOut.Stop();
            }
        }

        private NAudio.Wave.IWaveProvider GetInternalWaveProvider(AudioPlayerBackend.Player.IWaveProvider baseWP)
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
                //waveOut.Stop();
            }
            else if (waveProvider is IDisposable disposable) disposable.Dispose();
        }

        public void ExecutePlayState()
        {
            if (stop) return;

            switch (PlayState)
            {
                case PlaybackState.Stopped:
                    //waveOut.Stop();
                    break;

                case PlaybackState.Playing:
                    stopped = false;
                    //waveOut.Play();
                    break;

                case PlaybackState.Paused:
                    stopped = false;
                    //waveOut.Pause();
                    break;
            }
        }

        public void Dispose()
        {
            if (waveProvider is IDisposable disposable) disposable.Dispose();

            //waveOut.Dispose();
        }
    }
}
