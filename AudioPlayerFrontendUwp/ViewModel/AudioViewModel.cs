using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend
{
    class AudioViewModel : INotifyPropertyChanged
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

                    FileBasePlaylist = null;

                    AdditionalPlaylists.CollectionChanged -= AdditionalPlaylists_CollectionChanged1;
                }

                @base = value;

                if (@base != null)
                {
                    @base.PropertyChanged += Base_PropertyChanged;
                    @base.AdditionalPlaylists.CollectionChanged += AdditionalPlaylists_CollectionChanged;

                    FileBasePlaylist = new PlaylistViewModel(@base.FileBasePlaylist);

                    IEnumerable<PlaylistViewModel> playlists = @base.AdditionalPlaylists.Select(p => new PlaylistViewModel(p));
                    AdditionalPlaylists = new ObservableCollection<PlaylistViewModel>(playlists);
                    AdditionalPlaylists.CollectionChanged += AdditionalPlaylists_CollectionChanged1;
                }
                else
                {
                    FileBasePlaylist = null;
                    AdditionalPlaylists = null;
                }

                OnPropertyChanged(nameof(CurrentPlaylist));
            }
        }

        private void AdditionalPlaylists_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (PlaylistViewModel playlist in (IEnumerable)e.OldItems ?? Enumerable.Empty<PlaylistViewModel>())
            {
                Base.AdditionalPlaylists.Remove(playlist.Base);
            }

            foreach (PlaylistViewModel playlist in (IEnumerable)e.NewItems ?? Enumerable.Empty<PlaylistViewModel>())
            {
                if (!Base.AdditionalPlaylists.Contains(playlist.Base)) Base.AdditionalPlaylists.Add(playlist.Base);
            }
        }

        public bool IsOpenning => (Base as IMqttAudio)?.IsOpenning ?? false;

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

        public AudioViewModel(IAudioExtended @base)
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

        protected async void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            try
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                else await dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
            catch { }
        }
    }
}
