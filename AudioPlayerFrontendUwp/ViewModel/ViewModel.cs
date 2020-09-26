using System.ComponentModel;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
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
