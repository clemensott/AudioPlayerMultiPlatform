using System;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Join;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private ServiceBuild serviceOpenBuild;
        private IAudioService audioService;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;

        public ServiceBuild ServiceOpenBuild
        {
            get => serviceOpenBuild;
            set
            {
                if (value == serviceOpenBuild) return;

                serviceOpenBuild = value;
                OnPropertyChanged(nameof(ServiceOpenBuild));

                SetBuildResult(serviceOpenBuild);
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

        public ServiceBuild Build()
        {
            return ServiceOpenBuild = ServiceBuild.Build(Builder, TimeSpan.FromMilliseconds(200), AudioServiceHelper.Current);
        }

        public ServiceBuild Open()
        {
            return ServiceOpenBuild = ServiceBuild.Open(Communicator, AudioService, ServicePlayer, TimeSpan.FromMilliseconds(200));
        }

        private async void SetBuildResult(ServiceBuild build)
        {
            if (build == null) return;

            ServiceBuildResult result = await build.CompleteToken.ResultTask;

            if (build != ServiceOpenBuild) return;

            AudioService = result?.AudioService;
            Communicator = result?.Communicator;
            ServicePlayer = result?.ServicePlayer;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
