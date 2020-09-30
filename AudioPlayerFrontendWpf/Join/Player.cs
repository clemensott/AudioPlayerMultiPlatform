using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class Player : IPlayer
    {
        private bool stop, stopped;
        private RequestSong? wannaSong, nextWannaSong;
        private NAudio.Wave.WaveStream waveProvider;
        private PlaybackState playState;
        private readonly NAudio.Wave.WaveOut waveOut;
        private readonly SemaphoreSlim stopSem, handleSem;

        public event EventHandler<MediaOpenedEventArgs> MediaOpened;
        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

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

        public TimeSpan Position => waveProvider?.CurrentTime ?? TimeSpan.Zero;

        public TimeSpan Duration => waveProvider?.TotalTime ?? TimeSpan.Zero;

        public Song? Source { get; private set; }

        public Player(int deviceNumber = -1, IntPtr? windowHandle = null)
        {
            stop = false;
            stopped = true;
            stopSem = new SemaphoreSlim(1);
            handleSem = new SemaphoreSlim(1);

            waveOut = windowHandle.HasValue ? new NAudio.Wave.WaveOut(windowHandle.Value) : new NAudio.Wave.WaveOut();
            waveOut.DeviceNumber = deviceNumber;
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private async void WaveOut_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            await stopSem.WaitAsync();
            try
            {
                stopped = true;

                if (nextWannaSong.HasValue)
                {
                    stop = false;
                    DisposeWaveProvider();
                    Init(nextWannaSong.Value);
                    nextWannaSong = null;
                }
                else if (stop)
                {
                    stop = false;
                    DisposeWaveProvider();

                    ExecutePlayState();
                }
                else PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(Source, e.Exception));
            }
            finally
            {
                stopSem.Release();
            }
        }

        public Task Set(RequestSong? wanna)
        {
            return wanna.HasValue ? Set(wanna.Value) : Stop();
        }

        private Task Set(RequestSong wanna)
        {
            wannaSong = wanna;

            return Task.Run(async () =>
            {
                await handleSem.WaitAsync();
                try
                {
                    if (!wannaSong.Equals(wanna)) return;

                    await HandleRequestSong(wanna);
                }
                finally
                {
                    handleSem.Release();
                }
            });
        }

        private async Task HandleRequestSong(RequestSong wanna)
        {
            if (waveProvider != null)
            {
                if (Source.HasValue && wanna.Song.FullPath == Source?.FullPath)
                {
                    if (wanna.Position.HasValue &&
                        wanna.Position.Value != waveProvider.CurrentTime)
                    {
                        waveProvider.CurrentTime = wanna.Position.Value;
                    }
                    Source = wanna.Song;
                    return;
                }

                await Stop();
            }

            try
            {
                await BeginInit(wanna);
            }
            catch
            {
                DisposeWaveProvider();
            }
        }

        private async Task BeginInit(RequestSong wanna)
        {
            await stopSem.WaitAsync();
            try
            {
                if (stopped) Init(wanna);
                else
                {
                    DisposeWaveProvider();

                    nextWannaSong = wanna;
                    waveOut.Stop();
                }
            }
            finally
            {
                stopSem.Release();
            }
        }

        private void Init(RequestSong wanna)
        {
            try
            {
                waveProvider = new NAudio.Wave.AudioFileReader(wanna.Song.FullPath);
                Source = wanna.Song;

                if (wanna.Position.HasValue && wanna.Duration == waveProvider.TotalTime)
                {
                    waveProvider.CurrentTime = wanna.Position.Value;
                }

                waveOut.Init(waveProvider);
                ExecutePlayState();

                MediaOpened?.Invoke(this, new MediaOpenedEventArgs(waveProvider.CurrentTime, waveProvider.TotalTime, wanna.Song));
            }
            catch (Exception e)
            {
                DisposeWaveProvider();
                PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(wanna.Song, e));
            }
        }

        public async Task Stop()
        {
            await stopSem.WaitAsync();
            try
            {
                if (!stopped)
                {
                    stop = true;
                    nextWannaSong = null;
                    waveOut.Stop();
                }
                else DisposeWaveProvider();
            }
            finally
            {
                stopSem.Release();
            }
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
                    break;

                case PlaybackState.Paused:
                    waveOut.Pause();
                    stopped = waveOut.PlaybackState == NAudio.Wave.PlaybackState.Stopped;
                    break;
            }
        }

        private void DisposeWaveProvider()
        {
            waveProvider?.Dispose();
            waveProvider = null;
            Source = null;
        }

        public void Dispose()
        {
            DisposeWaveProvider();

            waveOut.Dispose();
        }
    }
}
