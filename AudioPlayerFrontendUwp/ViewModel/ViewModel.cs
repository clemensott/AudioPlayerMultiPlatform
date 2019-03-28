using System;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private BuildStatusToken buildOpenStatusToken;
        private IAudioService audioService;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;

        public bool IsTryOpening => BuildOpenStatusToken?.Task.IsCompleted == false;

        public BuildStatusToken BuildOpenStatusToken
        {
            get => buildOpenStatusToken;
            set
            {
                if (value == buildOpenStatusToken) return;

                buildOpenStatusToken = value;
                OnPropertyChanged(nameof(BuildOpenStatusToken));
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

        public ServiceBuilder Builder { get; }

        public ViewModel(ServiceBuilder builder)
        {
            Builder = builder;
        }

        public async Task<BuildEndedType> BuildAsync()
        {
            BuildOpenStatusToken = new BuildStatusToken();
            ServiceBuildResult result = await Builder.BuildWhileAsync(BuildOpenStatusToken, TimeSpan.FromMilliseconds(200));

            AudioService = result?.AudioService;
            Communicator = result?.Communicator;
            ServicePlayer = result?.ServicePlayer;

            return BuildOpenStatusToken.IsEnded ?? BuildEndedType.Successful;
        }

        public async Task<BuildEndedType> OpenAsync()
        {
            BuildOpenStatusToken = new BuildStatusToken();

            await ServiceBuilder.OpenWhileAsync(communicator, BuildOpenStatusToken, TimeSpan.FromMilliseconds(200));

            return BuildOpenStatusToken.IsEnded ?? BuildEndedType.Successful;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
