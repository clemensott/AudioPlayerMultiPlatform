using AudioPlayerBackend.Communication;
using StdOttStandard.Dispatch;
using StdOttStandard.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build
{
    public class AudioServicesHandler
    {
        private readonly Dispatcher backgrounTaskDispatcher;
        private readonly SemaphoreSlim keepOpenSem;
        private bool keepService;
        private AudioServicesBuilder builder;
        private AudioServices audioServices;

        public event EventHandler<AudioServicesBuilder> ServicesBuild;
        public event EventHandler<AudioServicesChangedEventArgs> AudioServicesChanged;
        public event EventHandler Stopped;

        public AudioServicesBuilder Builder
        {
            get => builder;
            set
            {
                if (value == builder) return;

                builder = value;
                ServicesBuild?.Invoke(this, builder);
            }
        }

        public AudioServices AudioServices
        {
            get => audioServices;
            set
            {
                if (value == audioServices) return;

                AudioServices oldServices = audioServices;
                audioServices = value;

                AudioServicesChanged?.Invoke(this, new AudioServicesChangedEventArgs(oldServices, value));
            }
        }

        public AudioServicesBuildConfig Config { get; private set; }

        public AudioServicesHandler(Dispatcher backgrounTaskDispatcher = null)
        {
            this.backgrounTaskDispatcher = backgrounTaskDispatcher;
            keepOpenSem = new SemaphoreSlim(0);
        }

        public async void Start(AudioServicesBuildConfig config)
        {
            Config = config;
            await Rebuild();
        }

        private async Task Rebuild()
        {
            await SetAudioServices(null);

            keepOpenSem.Release();
            StartKeepService();
        }

        private async void StartKeepService()
        {
            if (keepService) return;
            keepService = true;

            while (keepService)
            {
                await keepOpenSem.WaitAsync();

                await SetAudioServices(null);

                Builder = backgrounTaskDispatcher == null ? Build() : await backgrounTaskDispatcher.Run(Build);

                await SetAudioServices(await builder.CompleteToken.ResultTask);
            }

            AudioServicesBuilder Build()
            {
                return AudioServicesBuilder.Build(Config, TimeSpan.FromMilliseconds(500));
            }
        }

        private async Task SetAudioServices(AudioServices audioServices)
        {
            if (audioServices == this.audioServices) return;

            foreach (ICommunicator communicator in (this.audioServices?.GetCommunicators()).ToNotNull())
            {
                communicator.Disconnected -= OnDisconnected;
            }

            await (this.audioServices?.Dispose() ?? Task.CompletedTask);
            AudioServices = audioServices;

            foreach (ICommunicator communicator in (this.audioServices?.GetCommunicators()).ToNotNull())
            {
                communicator.Disconnected += OnDisconnected;
            }
        }

        private async void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            await Rebuild();
        }

        public async Task Stop()
        {
            await SetAudioServices(null);

            keepService = false;
            builder?.Cancel();

            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
