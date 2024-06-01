using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.ViewModels
{
    public class SongSearchViewModel : ISongSearchViewModel
    {
        public bool IsEnabled => throw new NotImplementedException();

        public bool IsSearching => throw new NotImplementedException();

        public bool IsSearchShuffle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string SearchKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<Song> SearchSongs => throw new NotImplementedException();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
