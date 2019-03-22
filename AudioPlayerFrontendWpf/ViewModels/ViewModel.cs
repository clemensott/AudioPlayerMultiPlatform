using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;
using System;
using System.ComponentModel;
using System.Windows;
using AudioPlayerBackend;

namespace AudioPlayerFrontend
{
    enum OpenState { Open, TryOpening, IDLE, Settings }

    class ViewModel : INotifyPropertyChanged
    {
        private bool isUiEnabled;
        private OpenState audioServiceState;
        private Visibility openVisibility, tryOpeningVisibility, idleVisibility;
        private Exception buildException;
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

        public OpenState AudioServiceState
        {
            get => audioServiceState;
            set
            {
                if (value == audioServiceState) return;

                audioServiceState = value;
                OnPropertyChanged(nameof(AudioServiceState));

                switch (value)
                {
                    case OpenState.Open:
                        OpenVisibility = Visibility.Visible;
                        TryOpeningVisibility = Visibility.Collapsed;
                        IdleVisibility = Visibility.Collapsed;
                        break;

                    case OpenState.TryOpening:
                        BuildException = null;
                        TryOpeningVisibility = Visibility.Visible;
                        IdleVisibility = Visibility.Collapsed;
                        OpenVisibility = Visibility.Hidden;
                        break;

                    case OpenState.IDLE:
                        IdleVisibility = Visibility.Visible;
                        TryOpeningVisibility = Visibility.Collapsed;
                        OpenVisibility = Visibility.Hidden;
                        break;
                }
            }
        }

        public Visibility OpenVisibility
        {
            get => openVisibility;
            set
            {
                if (value == openVisibility) return;

                openVisibility = value;
                OnPropertyChanged(nameof(OpenVisibility));
            }
        }

        public Visibility TryOpeningVisibility
        {
            get => tryOpeningVisibility;
            set
            {
                if (value == tryOpeningVisibility) return;

                tryOpeningVisibility = value;
                OnPropertyChanged(nameof(TryOpeningVisibility));
            }
        }

        public Visibility IdleVisibility
        {
            get => idleVisibility;
            set
            {
                if (value == idleVisibility) return;

                idleVisibility = value;
                OnPropertyChanged(nameof(IdleVisibility));
            }
        }

        public Exception BuildException
        {
            get => buildException;
            set
            {
                if (value == buildException) return;

                buildException = value;
                OnPropertyChanged(nameof(BuildException));
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

                if (oldService?.ServicePlayer != Service?.ServicePlayer)
                {
                    oldService?.ServicePlayer?.Dispose();

                    if (oldService?.ServicePlayer?.Player != Service?.ServicePlayer?.Player)
                    {
                        oldService?.ServicePlayer?.Player?.Dispose();
                    }
                }

                if (oldService?.Communicator != Service?.Communicator) oldService?.Communicator?.Dispose();
            }
        }

        public IAudioService AudioServiceUI => IsUiEnabled ? Service?.AudioService : null;

        public ICommunicator CommunicatorUI => IsUiEnabled ? Service?.Communicator : null;

        public IServicePlayer ServicePlayerUI => IsUiEnabled ? Service?.ServicePlayer : null;

        public ViewModel()
        {
            AudioServiceState = OpenState.TryOpening;
            IsUiEnabled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
