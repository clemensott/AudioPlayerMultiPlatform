using System.ComponentModel;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool isUpdatingPlaylists, isClient;

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

        public ViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            var del = PropertyChanged?.GetInvocationList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
