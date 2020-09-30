using System.ComponentModel;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool isUpdatingPlaylists;

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

        public ServiceHandler Service { get; }

        public ViewModel(ServiceHandler service)
        {
            Service = service;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
