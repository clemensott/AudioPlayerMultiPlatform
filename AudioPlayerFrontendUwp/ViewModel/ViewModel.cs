using AudioPlayerBackend;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool canceled;
        private Task<bool> buildOrOpenTask;
        private AudioViewModel audioService;

        public bool Canceled
        {
            get { return canceled; }
            private set
            {
                if (value == canceled) return;

                canceled = value;
                OnPropertyChanged(nameof(Canceled));
            }
        }

        public bool IsTryOpening => BuildOrOpenTask != null;

        public Task<bool> BuildOrOpenTask
        {
            get { return buildOrOpenTask; }
            private set
            {
                if (value == buildOrOpenTask) return;

                buildOrOpenTask = value;
                OnPropertyChanged(nameof(BuildOrOpenTask));
                OnPropertyChanged(nameof(IsTryOpening));
            }
        }

        public AudioViewModel AudioService
        {
            get { return audioService; }
            set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(AudioService));
            }
        }

        public ServiceBuilder Builder { get; private set; }

        public ViewModel(ServiceBuilder builder)
        {
            Builder = builder;
        }

        public async Task<bool> BuildAsync()
        {
            Canceled = false;

            Task<bool> task = Build();
            BuildOrOpenTask = task;

            bool built = await task;

            if (task == BuildOrOpenTask) BuildOrOpenTask = null;

            return built;
        }

        private async Task<bool> Build()
        {
            while (true)
            {
                try
                {
                    AudioService = new AudioViewModel(await Builder.Build());
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (!Canceled) continue;

                    AudioService = null;
                    break;
                }
            }

            return AudioService != null;
        }

        public async Task<bool> OpenAsync(IMqttAudio mqttAudio)
        {
            Canceled = false;

            Task<bool> task = Open(mqttAudio);
            BuildOrOpenTask = task;

            bool built = await task;

            if (task == BuildOrOpenTask) BuildOrOpenTask = null;

            return built;
        }

        public async Task<bool> Open(IMqttAudio mqttAudio)
        {
            if (mqttAudio == null || mqttAudio.IsOpen) return true;

            while (true)
            {
                try
                {
                    await mqttAudio.OpenAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (!Canceled) continue;

                    break;
                }
            }

            return mqttAudio.IsOpen;
        }

        public void CancelBuildOrOpen() => Canceled = true;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
