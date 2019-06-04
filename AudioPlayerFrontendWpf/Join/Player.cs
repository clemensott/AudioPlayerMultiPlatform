﻿using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerFrontend.Join
{
    class Player : IWaveProviderPlayer
    {
        public string debug;
        public bool stop, stopped;
        public NAudio.Wave.IWaveProvider waveProvider, nextWaveProvider;
        public readonly NAudio.Wave.WaveOut waveOut;
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
                DisposeWaveProvider();

                waveProvider = nextWaveProvider;
                nextWaveProvider = null;

                waveOut.Init(waveProvider);
                ExecutePlayState();
            }
            else if (stop)
            {
                DisposeWaveProvider();

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
                DisposeWaveProvider();

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
            else DisposeWaveProvider();
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
                    waveOut.Play();
                    stopped = false;
                    debug = "ExecutePlayState: Play, " + waveOut.PlaybackState;
                    break;

                case PlaybackState.Paused:
                    waveOut.Pause();
                    stopped = waveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped;
                    debug = "ExecutePlayState: Pause, " + waveOut.PlaybackState;
                    break;
            }
        }

        private void DisposeWaveProvider()
        {
            IDisposable disposable = waveProvider as IDisposable;
            waveProvider = null;
            disposable?.Dispose();
        }

        public void Dispose()
        {
            DisposeWaveProvider();

            waveOut.Dispose();
        }
    }
}
