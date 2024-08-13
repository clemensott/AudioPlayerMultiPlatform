using AudioPlayerBackend;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.ViewModels
{
    public class AddSourcePlaylistViewModel : INotifyPropertyChanged, IAudioService
    {
        private readonly IServicedLibraryRepo libraryRepo;
        public readonly IServicedPlaylistsRepo playlistsRepo;

        private string name;
        private string[] sources;
        private LoopType loop;
        private OrderType shuffle;
        private ObservableCollection<PlaylistInfo> sourcePlaylists;

        public string Name
        {
            get => name;
            set
            {
                if (value == name) return;

                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string[] Sources
        {
            get => sources;
            set
            {
                if (value == sources) return;

                sources = value;
                OnPropertyChanged(nameof(Sources));
            }
        }

        public LoopType Loop
        {
            get => loop;
            set
            {
                if (value == loop) return;

                loop = value;
                OnPropertyChanged(nameof(Loop));
            }
        }

        public OrderType Shuffle
        {
            get => shuffle;
            set
            {
                if (value == shuffle) return;

                shuffle = value;
                OnPropertyChanged(nameof(Shuffle));
            }
        }

        public ObservableCollection<PlaylistInfo> SourcePlaylists
        {
            get => sourcePlaylists;
            set
            {
                if (value == sourcePlaylists) return;

                sourcePlaylists = value;
                OnPropertyChanged(nameof(SourcePlaylists));
            }
        }

        public AddSourcePlaylistViewModel(AudioServices audioServices)
        {
            libraryRepo = audioServices.GetServicedLibraryRepo();
            playlistsRepo = audioServices.GetServicedPlaylistsRepo();

            Loop = LoopType.CurrentPlaylist;
        }

        public Task Start()
        {
            // Sync data and subscribe to events
            throw new System.NotImplementedException();
        }

        public Task Stop()
        {
            // Unsubscribe to events
            throw new System.NotImplementedException();
        }

        public async Task Dispose()
        {
            await Stop();

            await libraryRepo.Dispose();
            await playlistsRepo.Dispose();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
