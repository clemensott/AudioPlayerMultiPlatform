using System.ComponentModel;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool isTryOpening, viewAdvancedSettings;
        private AudioViewModel audioService;

        public bool IsTryOpening
        {
            get { return isTryOpening; }
            set
            {
                if (value == isTryOpening) return;

                isTryOpening = value;
                OnPropertyChanged(nameof(IsTryOpening));
            }
        }

        public bool ViewAdvancedSettings
        {
            get { return viewAdvancedSettings; }
            set
            {
                if (value == viewAdvancedSettings) return;

                viewAdvancedSettings = value;
                OnPropertyChanged(nameof(ViewAdvancedSettings));
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
