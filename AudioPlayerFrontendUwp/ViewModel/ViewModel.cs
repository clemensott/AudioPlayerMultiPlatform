using AudioPlayerBackend.Audio;
using System.ComponentModel;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool isUpdatingPlaylists, isClient;
        private IAudioService audio;

        public bool IsUpdatingPlaylists
        {
            get => isUpdatingPlaylists;
            set
            {
                if (value == isUpdatingPlaylists) return;

                isUpdatingPlaylists = value;
                OnPropertyChanged(nameof(IsUpdatingPlaylists));
            }
        }

        public bool IsClient
        {
            get => isClient;
            set
            {
                if (value == isClient) return;

                isClient = value;
                OnPropertyChanged(nameof(IsClient));
            }
        }

        public IAudioService Audio
        {
            get => audio;
            set
            {
                if (value == audio) return;

                audio = value;
                OnPropertyChanged(nameof(Audio));
            }
        }

        public ViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
