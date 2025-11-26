using System.Collections.ObjectModel;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.ViewModels;
using AudioPlayerFrontendAvalonia.Models;

namespace AudioPlayerFrontendAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ILibraryViewModel? library;

    public ILibraryViewModel? Library
    {
        get => library;
        set
        {
            if (value == library) return;

            library = value;
            OnPropertyChanged();
        }
    }

    public static ObservableCollection<ListOption<OrderType>> ShuffleOptions =
    [
        new(OrderType.ByTitleAndArtist, "Title and Artist"),
        new(OrderType.ByPath, "File path"),
        new(OrderType.Custom, "Shuffle"),
    ];

    public static ObservableCollection<ListOption<LoopType>> LoopOptions =
    [
        new(LoopType.Next, "Next playlist"),
        new(LoopType.Stop, "Next playlist and stop"),
        new(LoopType.CurrentPlaylist, "Repeat playlist"),
        new(LoopType.CurrentSong, "Repeat current song"),
        new(LoopType.StopCurrentSong, "Next song and stop"),
    ];

    public static ObservableCollection<ListOption<double>> PlaybackRateOptions =
    [
        new(0.5, "0.5x"),
        new(0.7, "0.7x"),
        new(0.9, "0.9x"),
        new(1, "1x"),
        new(1.15, "1.15x"),
        new(1.3, "1.3x"),
        new(1.5, "1.5x"),
        new(1.75, "1.75x"),
        new(2, "2x"),
        new(2.25, "2.25x"),
        new(2.5, "2.5x"),
    ];
}