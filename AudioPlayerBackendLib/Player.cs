using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AudioPlayerBackendLib
{
    class Player
    {
        private static Dictionary<IntPtr?, Player> players;

        public static Player GetPlayer(IntPtr? windowHandler)
        {
            if (players == null) players = new Dictionary<IntPtr?, Player>();

            Player player;
            if (!players.TryGetValue(windowHandler, out player))
            {
                player = new Player(windowHandler);
                players.Add(windowHandler, player);
            }

            return player;
        }

        private readonly WaveOut wave;
        private Thread callbackThread;
        private Queue<IDisposable> disposeReaders;
        private PlaybackState playState;

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

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
            get { return wave.Volume; }
            set { wave.Volume = value; }
        }

        private Player(IntPtr? windowHandler)
        {
            callbackThread = Thread.CurrentThread;
            wave = windowHandler.HasValue ? new WaveOut(windowHandler.Value) : new WaveOut();
            wave.PlaybackStopped += Wave_PlaybackStopped;

            disposeReaders = new Queue<IDisposable>();
        }

        private void Wave_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (disposeReaders.Count > 0)
            {
                lock (disposeReaders)
                {
                    while (disposeReaders.Count > 0)
                    {
                        disposeReaders.Dequeue().Dispose();
                    }

                    Monitor.Pulse(disposeReaders);
                }

                ExecutePlayState();
            }
            else PlaybackStopped?.Invoke(this, e);
        }

        public void Init(IWaveProvider waveProvider)
        {
            wave.Init(waveProvider);

            ExecutePlayState();
        }

        public void DisposeReader(WaveStream reader)
        {
            if (wave.PlaybackState != PlaybackState.Stopped)
            {
                lock (disposeReaders)
                {
                    disposeReaders.Enqueue(reader);
                    wave.Stop();

                    if (callbackThread != Thread.CurrentThread) Monitor.Wait(disposeReaders);
                }
            }
            else reader.Dispose();
        }

        public void DisposeReaders(IEnumerable<WaveStream> readers)
        {
            if (wave.PlaybackState != PlaybackState.Stopped)
            {
                lock (disposeReaders)
                {
                    foreach (WaveStream reader in readers) disposeReaders.Enqueue(reader);

                    wave.Stop();

                    if (disposeReaders.Count > 0) Monitor.Wait(disposeReaders);
                }
            }
            else foreach (WaveStream reader in readers) reader.Dispose();
        }

        private void ExecutePlayState()
        {
            if (disposeReaders.Count > 0) return;

            try
            {
                if (PlayState == PlaybackState.Paused) wave.Pause();
                else if (PlayState == PlaybackState.Playing) wave.Play();
                else if (PlayState == PlaybackState.Stopped) wave.Stop();
            }
            catch { }
        }
    }
}
