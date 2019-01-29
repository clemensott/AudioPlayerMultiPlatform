using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private IAudioExtended @base;
        private PlaylistViewModel fileBasePlaylist;
        private ObservableCollection<PlaylistViewModel> additionalPlaylists;

        public IAudioExtended Base
        {
            get => @base;
            set
            {
                if (value == @base) return;

                if (@base != null)
                {
                    @base.PropertyChanged -= Base_PropertyChanged;
                    @base.AdditionalPlaylists.CollectionChanged -= AdditionalPlaylists_CollectionChanged;
                }

                @base = value;

                if (@base != null)
                {
                    @base.PropertyChanged += Base_PropertyChanged;
                    @base.AdditionalPlaylists.CollectionChanged += AdditionalPlaylists_CollectionChanged;

                    FileBasePlaylist = new PlaylistViewModel(@base.FileBasePlaylist);

                    IEnumerable<PlaylistViewModel> playlists = @base.AdditionalPlaylists.Select(p => new PlaylistViewModel(p));
                    AdditionalPlaylists = new ObservableCollection<PlaylistViewModel>(playlists);
                }
                else
                {
                    FileBasePlaylist = null;
                    AdditionalPlaylists = null;
                }

                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        public PlaybackState PlayState { get => Base.PlayState; set => Base.PlayState = value; }

        public string[] FileMediaSources { get => Base.FileMediaSources; set => Base.FileMediaSources = value; }

        public float Volume { get => Base.Volume; set => Base.Volume = value; }

        public IPlayer Player { get => Base.Player; }

        public PlaylistViewModel CurrentPlaylist
        {
            get => GetPlaylistViewModel(Base.CurrentPlaylist);
            set => Base.CurrentPlaylist = value.Base;
        }

        public PlaylistViewModel FileBasePlaylist
        {
            get { return fileBasePlaylist; }
            private set
            {
                if (value == fileBasePlaylist) return;

                fileBasePlaylist = value;
                OnPropertyChanged(nameof(FileBasePlaylist));
            }
        }

        public ObservableCollection<PlaylistViewModel> AdditionalPlaylists
        {
            get { return additionalPlaylists; }
            private set
            {
                if (value == additionalPlaylists) return;

                additionalPlaylists = value;
                OnPropertyChanged(nameof(AdditionalPlaylists));
            }
        }

        public ViewModel(IAudioExtended @base)
        {
            additionalPlaylists = new ObservableCollection<PlaylistViewModel>();

            Base = @base;
        }

        public void SetNextSong() => Base.SetNextSong();

        public void SetPreviousSong() => Base.SetPreviousSong();

        public void Reload() => Base.Reload();

        public void Dispose() => Base.Dispose();

        public IEnumerable<IPlaylistExtended> GetAllPlaylists() => Base.GetAllPlaylists();

        public IPlaylistExtended GetPlaylist(Guid id) => Base.GetPlaylist(id);

        private void AdditionalPlaylists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IPlaylistExtended newPlaylist = e.NewItems?.OfType<IPlaylistExtended>().FirstOrDefault();
            IPlaylistExtended oldPlaylist = e.OldItems?.OfType<IPlaylistExtended>().FirstOrDefault();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AdditionalPlaylists.Insert(e.NewStartingIndex, new PlaylistViewModel(newPlaylist));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (AdditionalPlaylists[e.OldStartingIndex].Base != oldPlaylist) { }
                    AdditionalPlaylists.RemoveAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    AdditionalPlaylists[e.NewStartingIndex] = new PlaylistViewModel(newPlaylist);
                    break;

                case NotifyCollectionChangedAction.Move:
                    AdditionalPlaylists.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    AdditionalPlaylists.Clear();

                    foreach (IPlaylistExtended playlist in Base.AdditionalPlaylists)
                    {
                        AdditionalPlaylists.Add(new PlaylistViewModel(playlist));
                    }
                    break;
            }
        }

        private PlaylistViewModel GetPlaylistViewModel(IPlaylistExtended playlist)
        {
            if (FileBasePlaylist.Base == playlist) return FileBasePlaylist;

            return AdditionalPlaylists.First(p => p.Base == playlist);
        }

        private void Base_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
