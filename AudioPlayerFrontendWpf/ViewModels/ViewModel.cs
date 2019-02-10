using System;
using System.ComponentModel;
using System.Windows;

namespace AudioPlayerFrontend
{
    enum OpenState { Open, TryOpening, IDLE, Settings }

    class ViewModel : INotifyPropertyChanged
    {
        private OpenState audioServiceState;
        private Visibility openVisibility, tryOpeningVisibility, idleVisibility;
        private Exception buildException;
        private AudioViewModel audioService;
        private bool isUiEnabled;


        public OpenState AudioServiceState
        {
            get { return audioServiceState; }
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
            get { return openVisibility; }
            set
            {
                if (value == openVisibility) return;

                openVisibility = value;
                OnPropertyChanged(nameof(OpenVisibility));
            }
        }

        public Visibility TryOpeningVisibility
        {
            get { return tryOpeningVisibility; }
            set
            {
                if (value == tryOpeningVisibility) return;

                tryOpeningVisibility = value;
                OnPropertyChanged(nameof(TryOpeningVisibility));
            }
        }

        public Visibility IdleVisibility
        {
            get { return idleVisibility; }
            set
            {
                if (value == idleVisibility) return;

                idleVisibility = value;
                OnPropertyChanged(nameof(IdleVisibility));
            }
        }

        public Exception BuildException
        {
            get { return buildException; }
            set
            {
                if (value == buildException) return;

                buildException = value;
                OnPropertyChanged(nameof(BuildException));
            }
        }

        public AudioViewModel AudioService
        {
            get { return audioService ; }
            set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(AudioService));
                OnPropertyChanged(nameof(AudioServiceUI));
            }
        }

        public AudioViewModel AudioServiceUI
        {
            get { return isUiEnabled ? AudioService : null; }
        }

        public bool IsUiEnabled
        {
            get { return isUiEnabled; }
            set
            {
                if (value == isUiEnabled) return;

                isUiEnabled = value;
                OnPropertyChanged(nameof(IsUiEnabled));
                OnPropertyChanged(nameof(AudioServiceUI));
            }
        }

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
