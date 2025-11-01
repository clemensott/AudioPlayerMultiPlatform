using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
using AudioPlayerBackend.Build;
using StdOttStandard.Linq;

namespace AudioPlayerFrontendAvalonia.ViewModels;

public class AddSourcePlaylistViewModel : ViewModelBase
{
    private bool appendSources, isNewPlaylistChecked, newPlaylist, isFormValid;
    private string name;
    private string sources;
    private LoopType loop;
    private OrderType shuffle;
    private PlaylistInfo? selectedPlaylist;
    private ObservableCollection<PlaylistInfo> sourcePlaylists;

    public bool IsNewPlaylistChecked
    {
        get => isNewPlaylistChecked;
        set
        {
            if (value == isNewPlaylistChecked) return;

            isNewPlaylistChecked = value;
            OnPropertyChanged(nameof(IsNewPlaylistChecked));

            NewPlaylist = isNewPlaylistChecked || SourcePlaylists.Count == 0;
        }
    }

    public bool NewPlaylist
    {
        get => newPlaylist;
        set
        {
            if (value == newPlaylist) return;

            newPlaylist = value;
            OnPropertyChanged(nameof(NewPlaylist));

            UpdateIsFormValid();
        }
    }

    public bool IsFormValid
    {
        get => isFormValid;
        set
        {
            if (value == isFormValid) return;

            isFormValid = value;
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    public bool AppendSources
    {
        get => appendSources;
        set
        {
            if (value == appendSources) return;

            appendSources = value;
            OnPropertyChanged(nameof(AppendSources));
        }
    }

    public string Name
    {
        get => name;
        set
        {
            if (value == name) return;

            name = value;
            OnPropertyChanged(nameof(Name));

            UpdateIsFormValid();
        }
    }

    public string Sources
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

    public PlaylistInfo? SelectedPlaylist
    {
        get => selectedPlaylist;
        set
        {
            if (value == selectedPlaylist) return;

            selectedPlaylist = value;
            OnPropertyChanged(nameof(SelectedPlaylist));

            UpdateIsFormValid();
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

            UpdateIsFormValid();
        }
    }

    public ILibraryRepo LibraryRepo { get; }

    public IPlaylistsRepo PlaylistsRepo { get; }

    public AddSourcePlaylistViewModel()
    {
    }

    public AddSourcePlaylistViewModel(AudioServices audioServices)
    {
        LibraryRepo = audioServices.GetLibraryRepo();
        PlaylistsRepo = audioServices.GetPlaylistsRepo();

        Loop = LoopType.CurrentPlaylist;
    }

    public async Task Start()
    {
        PlaylistsRepo.InsertedPlaylist += PlaylistsRepo_InsertedPlaylist;
        PlaylistsRepo.RemovedPlaylist += PlaylistsRepo_RemovedPlaylist;

        Library library = await LibraryRepo.GetLibrary();
        SourcePlaylists = new ObservableCollection<PlaylistInfo>(library.Playlists);
    }

    public Task Stop()
    {
        PlaylistsRepo.InsertedPlaylist -= PlaylistsRepo_InsertedPlaylist;
        PlaylistsRepo.RemovedPlaylist -= PlaylistsRepo_RemovedPlaylist;

        SourcePlaylists.Clear();

        return Task.CompletedTask;
    }

    private void PlaylistsRepo_InsertedPlaylist(object sender, InsertPlaylistArgs e)
    {
        SourcePlaylists.Insert(e.Index ?? SourcePlaylists.Count, e.Playlist.ToPlaylistInfo());

        UpdateIsFormValid();
    }

    private void PlaylistsRepo_RemovedPlaylist(object sender, RemovePlaylistArgs e)
    {
        int index = SourcePlaylists.IndexOf(p => p.Id == e.Id);
        SourcePlaylists.RemoveAt(index);

        UpdateIsFormValid();
    }

    private void UpdateIsFormValid()
    {
        IsFormValid = NewPlaylist ? !string.IsNullOrWhiteSpace(Name) : SelectedPlaylist != null;
    }

    public async Task Dispose()
    {
        await Stop();
    }
}