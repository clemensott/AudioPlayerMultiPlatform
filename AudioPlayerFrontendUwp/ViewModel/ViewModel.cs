using System;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.MQTT;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool canceled;
        private Task<bool> buildOrOpenTask;
        private IAudioService audioService;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;

        public bool Canceled
        {
            get => canceled;
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
            get => buildOrOpenTask;
            private set
            {
                if (value == buildOrOpenTask) return;

                buildOrOpenTask = value;
                OnPropertyChanged(nameof(BuildOrOpenTask));
                OnPropertyChanged(nameof(IsTryOpening));
            }
        }

        public IAudioService AudioService
        {
            get => audioService;
            set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(AudioService));
            }
        }

        public ICommunicator Communicator
        {
            get => communicator;
            set
            {
                if (value == communicator) return;

                communicator = value;
                OnPropertyChanged(nameof(Communicator));
            }
        }

        public IServicePlayer ServicePlayer
        {
            get => servicePlayer;
            set
            {
                if (value == servicePlayer) return;

                servicePlayer = value;
                OnPropertyChanged(nameof(ServicePlayer));
            }
        }

        public ServiceBuilder Builder { get; private set; }

        public ViewModel(ServiceBuilder builder)
        {
            System.Diagnostics.Debug.WriteLine("ViewModelCtor");
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
                    ServiceBuildResult result = await Builder.Build();

                    AudioService =result.AudioService;
                    Communicator = result.Communicator;
                    ServicePlayer = result.ServicePlayer;
                    break;
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    await Task.Delay(200);

                    if (!Canceled) continue;

                    AudioService = null;
                    break;
                }
            }

            return AudioService != null;
        }

        public async Task<bool> OpenAsync(ICommunicator communicator)
        {
            Canceled = false;

            Task<bool> task = Open(communicator);
            BuildOrOpenTask = task;

            bool built = await task;

            if (task == BuildOrOpenTask) BuildOrOpenTask = null;

            return built;
        }

        public async Task<bool> Open(ICommunicator communicator)
        {
            if (communicator == null || communicator.IsOpen) return true;

            while (true)
            {
                try
                {
                    await communicator.OpenAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (!Canceled) continue;

                    break;
                }
            }

            return communicator.IsOpen;
        }

        public void CancelBuildOrOpen() => Canceled = true;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
