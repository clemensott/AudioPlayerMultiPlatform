using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;
using System.ComponentModel;
using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend
{
    public class ViewModel : INotifyPropertyChanged
    {
        private bool isUiEnabled;
        private BuildStatusToken buildOpenStatusToken;
        private ServiceBuildResult service;

        public bool IsUiEnabled
        {
            get => isUiEnabled;
            set
            {
                if (value == isUiEnabled) return;

                isUiEnabled = value;
                OnPropertyChanged(nameof(IsUiEnabled));
                OnPropertyChanged(nameof(AudioServiceUI));
                OnPropertyChanged(nameof(CommunicatorUI));
                OnPropertyChanged(nameof(ServicePlayerUI));
            }
        }

        public BuildStatusToken BuildOpenStatusToken
        {
            get => buildOpenStatusToken;
            set
            {
                if (value == buildOpenStatusToken) return;

                buildOpenStatusToken = value;
                OnPropertyChanged(nameof(BuildOpenStatusToken));
            }
        }

        public ServiceBuildResult Service
        {
            get => service;
            set
            {
                if (value == service) return;

                ServiceBuildResult oldService = Service;

                service = value;
                OnPropertyChanged(nameof(Service));
                OnPropertyChanged(nameof(AudioServiceUI));
                OnPropertyChanged(nameof(CommunicatorUI));
                OnPropertyChanged(nameof(ServicePlayerUI));

                if (oldService?.ServicePlayer != Service?.ServicePlayer) oldService?.ServicePlayer?.Dispose();
                if (oldService?.Communicator != Service?.Communicator) oldService?.Communicator?.Dispose();
                if (oldService?.Data != Service?.Data) oldService?.Data?.Dispose();
            }
        }

        public IAudioService AudioServiceUI => IsUiEnabled ? Service?.AudioService : null;

        public ICommunicator CommunicatorUI => IsUiEnabled ? Service?.Communicator : null;

        public IPlayerService ServicePlayerUI => IsUiEnabled ? Service?.ServicePlayer : null;

        public ViewModel()
        {
            IsUiEnabled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
