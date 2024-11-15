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
        private readonly object isStartedLockObj;
        private readonly Dispatcher backgrounTaskDispatcher;
        private readonly SemaphoreSlim keepOpenSem;
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

        public bool IsStarted { get; private set; }

        public AudioServicesHandler(Dispatcher backgrounTaskDispatcher = null)
        {
            isStartedLockObj = new object();
            this.backgrounTaskDispatcher = backgrounTaskDispatcher;
            keepOpenSem = new SemaphoreSlim(0);
        }

        public void AddServiceBuildListener(EventHandler<AudioServicesBuilder> eventHandler)
        {
            ServicesBuild += eventHandler;
            if (Builder != null) eventHandler(this, Builder);
        }

        public void AddAudioServicesChangedListener(EventHandler<AudioServicesChangedEventArgs> eventHandler)
        {
            AudioServicesChanged += eventHandler;
            if (AudioServices != null) eventHandler(this, new AudioServicesChangedEventArgs(null, AudioServices));
        }

        public async void Start(AudioServicesBuildConfig config = null)
        {
            if (config != null) Config = config;
            if (Config != null) await Rebuild();
        }

        private async Task Rebuild()
        {
            await SetAudioServices(null);

            keepOpenSem.Release();
            StartKeepService();
        }

        private async void StartKeepService()
        {
            lock (isStartedLockObj)
            {
                if (IsStarted) return;
                IsStarted = true;
            }

            while (IsStarted)
            {
                await keepOpenSem.WaitAsync();

                await SetAudioServices(null);

                Builder = new AudioServicesBuilder(Config.Clone());
                if (backgrounTaskDispatcher == null) Build();
                else await backgrounTaskDispatcher.Run(Build);

                await SetAudioServices(await builder.CompleteToken.ResultTask);
            }

            void Build()
            {
                Builder.StartBuild(TimeSpan.FromMilliseconds(500));
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

            lock (isStartedLockObj) IsStarted = false;
            Builder?.Cancel();

            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
