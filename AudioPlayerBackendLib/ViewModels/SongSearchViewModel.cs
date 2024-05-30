using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AudioPlayerBackend.ViewModels
{
    public class SongSearchViewModel : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
