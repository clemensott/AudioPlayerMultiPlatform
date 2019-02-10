using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private readonly Queue<NAudio.Wave.IWaveProvider> playWaveProviders;
        private readonly Queue<IDisposable> disposeObjs;
        private NAudio.Wave.WaveOut waveOut;
        private int waveProvidersCount;
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
            waveProvidersCount = 0;
            playWaveProviders = new Queue<NAudio.Wave.IWaveProvider>();
            disposeObjs = new Queue<IDisposable>();

            waveOut = windowHandle.HasValue ? new NAudio.Wave.WaveOut(windowHandle.Value) : new NAudio.Wave.WaveOut();

            waveOut.DeviceNumber = deviceNumber;
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            lock (disposeObjs)
            {
                if (disposeObjs.Count == 0 && playWaveProviders.Count == 0)
                {
                    PlaybackStopped?.Invoke(this, e.ToBackend());
                    return;
                }

                while (disposeObjs.Count > 0)
                {
                    waveProvidersCount--;
                    disposeObjs.Dequeue().Dispose();
                }

                while (playWaveProviders.Count > 0)
                {
                    waveOut.Init(playWaveProviders.Dequeue());
                    waveProvidersCount++;
                }

                ExecutePlayState();
            }
        }

        public void Play(Func<AudioPlayerBackend.Common.IWaveProvider> waveProviderFunc)
        {
            NAudio.Wave.IWaveProvider wp = GetInternalWaveProvider(waveProviderFunc());

            lock (disposeObjs)
            {
                if (disposeObjs.Count > 0) playWaveProviders.Enqueue(wp);
                else
                {
                    waveOut.Init(wp);
                    waveProvidersCount++;
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

        public void Stop(IDisposable disposeObj)
        {
            if (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
            {
                waveProvidersCount--;
                disposeObj.Dispose();
            }
            else
            {
                disposeObjs.Enqueue(disposeObj);
                waveOut.Stop();
            }
        }

        public void ExecutePlayState()
        {
            if (disposeObjs.Count > 0 || waveProvidersCount == 0) return;

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
