using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using NAudio.CoreAudioApi;
using NAudio.Win8.Wave.WaveOutputs;
using System;
using System.Collections.Generic;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private readonly Queue<Func<NAudio.Wave.IWaveProvider>> playWaveProviderFuncs;
        private readonly Queue<IDisposable> disposeObjs;
        private WasapiOutRT waveOut;
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

        public float Volume { get { return 1; } set { } }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public Player()
        {
            playWaveProviderFuncs = new Queue<Func<NAudio.Wave.IWaveProvider>>();
            disposeObjs = new Queue<IDisposable>();

            waveOut = new WasapiOutRT(AudioClientShareMode.Shared, 200);

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            lock (disposeObjs)
            {
                if (disposeObjs.Count == 0 && playWaveProviderFuncs.Count == 0)
                {
                    PlaybackStopped?.Invoke(this, e.ToBackend());
                    return;
                }

                while (disposeObjs.Count > 0) disposeObjs.Dequeue().Dispose();
                while (playWaveProviderFuncs.Count > 0) waveOut.Init(playWaveProviderFuncs.Dequeue());

                ExecutePlayState();
            }
        }

        public void Play(Func<AudioPlayerBackend.Common.IWaveProvider> waveProviderFunc)
        {
            NAudio.Wave.IWaveProvider wpf()
            {
                return GetInternalWaveProvider(waveProviderFunc());
            }

            lock (disposeObjs)
            {
                if (disposeObjs.Count > 0) playWaveProviderFuncs.Enqueue(wpf);
                else
                {
                    waveOut.Init(wpf);
                    ExecutePlayState();
                }
            }
        }

        private NAudio.Wave.IWaveProvider GetInternalWaveProvider(AudioPlayerBackend.Common.IWaveProvider baseWP)
        {
            switch (baseWP)
            {
                case NAudio.Wave.IWaveProvider iwp:
                    return iwp;

                default:
                    return new WaveProvider(baseWP);
            }
        }

        public void Stop(IDisposable dispose)
        {
            if (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped) dispose.Dispose();
            else
            {
                disposeObjs.Enqueue(dispose);
                waveOut.Stop();
            }
        }

        public void ExecutePlayState()
        {
            if (disposeObjs.Count > 0) return;

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
