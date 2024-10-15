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
        private AudioServicesBuildConfig config;
        private AudioServicesBuilder builder;
        private AudioServices audioServices;

        public event EventHandler<AudioServicesBuilder> ServicesBuild;
        public event EventHandler Stopped;


        public AudioServicesHandler(Dispatcher backgrounTaskDispatcher = null)
        {
            this.backgrounTaskDispatcher = backgrounTaskDispatcher;
            keepOpenSem = new SemaphoreSlim(0);
        }

        public async void Start(AudioServicesBuildConfig config)
        {
            this.config = config;
            await Rebuild();
        }

        private async Task Rebuild()
        {
            SetAudioServices(null);

            await (audioServices?.Dispose() ?? Task.CompletedTask);
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

                SetAudioServices(null);

                builder = backgrounTaskDispatcher == null ? Build() : await backgrounTaskDispatcher.Run(Build);
                ServicesBuild?.Invoke(this, builder);

                SetAudioServices(await builder.CompleteToken.ResultTask);
            }

            AudioServicesBuilder Build()
            {
                return AudioServicesBuilder.Build(config, TimeSpan.FromMilliseconds(5000));
            }
        }

        private async Task SetAudioServices(AudioServices audioServices)
        {
            foreach (ICommunicator communicator in (this.audioServices?.GetCommunicators()).ToNotNull())
            {
                communicator.Disconnected -= OnDisconnected;
            }

            await (this.audioServices?.Dispose() ?? Task.CompletedTask);
            this.audioServices = audioServices;

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
