using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AudioPlayerBackend;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using AudioPlayerBackend.ViewModels;
using AudioPlayerFrontendAvalonia.Join;
using AudioPlayerFrontendAvalonia.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StdOttStandard;
using StdOttStandard.Linq;

namespace AudioPlayerFrontendAvalonia.Views;

public partial class MainWindow : Window
{
    private readonly AudioServicesHandler audioServicesHandler;
    private AudioServicesBuildConfig servicesBuildConfig;
    private AudioServices? audioServices;
    private ILibraryViewModel? viewModel;
    private int? nextLbxSearchSongsSelectedIndex;

    public MainWindow()
    {
        InitializeComponent();

        audioServicesHandler = new AudioServicesHandler();
        servicesBuildConfig = new AudioServicesBuildConfig();

        lbxSearchSongs.Items.CollectionChanged += LbxSearchSongs_OnCollectionChanged;
    }

    private async void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        try
        {
            servicesBuildConfig.WithArgs(args);
        }
        catch (Exception exc)
        {
            await DialogWindow.ShowPrimary(this, exc.Message, "Create service builder error");
            Close();
            return;
        }

        servicesBuildConfig.AdditionalServices.TryAddSingleton<IPlayer, Player>();
        servicesBuildConfig.AdditionalServices.TryAddSingleton<IFileSystemService, FileSystemService>();
        servicesBuildConfig.AdditionalServices.TryAddSingleton<IInvokeDispatcherService, InvokeDispatcherService>();
        servicesBuildConfig.AdditionalServices.TryAddSingleton<IUpdateLibraryService, UpdateLibraryService>();

        audioServicesHandler.ServicesBuild += AudioServicesHandler_ServicesBuild;
        audioServicesHandler.Stopped += AudioServicesHandler_Stopped;

        audioServicesHandler.Start(servicesBuildConfig);
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
        audioServicesHandler.Stopped -= AudioServicesHandler_Stopped;

        await audioServicesHandler.Stop();
    }

    private async void AudioServicesHandler_ServicesBuild(object? sender, AudioServicesBuilder? build)
    {
        if (build == null) return;

        await Task.WhenAny(build.CompleteToken.ResultTask, Task.Delay(100));

        if (!build.CompleteToken.IsEnded.HasValue) await ShowBuildOpenWindow(build);

        switch (build.CompleteToken.IsEnded)
        {
            case BuildEndedType.Canceled:
                Close();
                return;

            case BuildEndedType.Settings:
                await StopAndUpdateBuilder();
                return;
        }

        AudioServices newAudioServices = await build.CompleteToken.ResultTask;
        if (newAudioServices != null)
        {
            audioServices = newAudioServices;
            viewModel = audioServices.GetViewModel();
            if (DataContext is MainWindowViewModel mainWindowViewModel) mainWindowViewModel.Library = viewModel;
        }
    }

    private async Task StopAndUpdateBuilder()
    {
        await audioServicesHandler.Stop();

        try
        {
            await UpdateBuildConfig();
        }
        finally
        {
            audioServicesHandler.Start(servicesBuildConfig);
        }
    }

    private async Task<bool> UpdateBuildConfig()
    {
        AudioServicesBuildConfig editServicesBuildConfig = servicesBuildConfig.Clone();
        SettingsWindow window = new SettingsWindow(editServicesBuildConfig);
        bool submitted = await window.ShowDialog<bool>(this);
        if (submitted) servicesBuildConfig = editServicesBuildConfig;

        return submitted;
    }

    private void AudioServicesHandler_Stopped(object? sender, EventArgs e)
    {
    }

    private async Task ShowBuildOpenWindow(AudioServicesBuilder build)
    {
        BuildOpenWindow window = new BuildOpenWindow(build);
        await window.ShowDialog(this);
    }

    private object? PlaylistMenuItemVisCon_OnConvertEvent(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        PlaylistType? playlistType = (PlaylistType?)value;
        return viewModel?.IsLocalFileMediaSource == true && playlistType?.HasFlag(PlaylistType.SourcePlaylist) == true;
    }

    private void LbxSongs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        IPlaylistViewModel? currentPlaylist = viewModel?.CurrentPlaylist;
        if (e.AddedItems.TryFirst(out object added)
            && added is Song song
            && currentPlaylist != null
            && currentPlaylist.CurrentSongRequest?.Id != song.Id)
        {
            currentPlaylist.CurrentSongRequest = SongRequest.Start(song.Id);
        }

        Scroll();
    }

    private async void LbxSearchSongs_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await Task.Delay(10);
        if (nextLbxSearchSongsSelectedIndex.HasValue)
        {
            lbxSearchSongs.SelectedIndex =
                Math.Min(nextLbxSearchSongsSelectedIndex.Value, lbxSearchSongs.Items.Count - 1);
            nextLbxSearchSongsSelectedIndex = null;
        }
        else if (lbxSearchSongs.SelectedIndex == -1 && lbxSearchSongs.Items.Count > 0) lbxSearchSongs.SelectedIndex = 0;
    }

    private async void TbxSearch_KeyDown(object? sender, KeyEventArgs e)
    {
        if (viewModel == null) return;
        e.Handled = true;

        switch (e.Key)
        {
            case Key.Enter:
                if (lbxSearchSongs.SelectedItem is Song addSong)
                {
                    SearchPlaylistAddType addType;
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    {
                        addType = SearchPlaylistAddType.FirstInPlaylist;
                    }
                    else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        addType = SearchPlaylistAddType.NextInPlaylist;
                    }
                    else addType = SearchPlaylistAddType.LastInPlaylist;

                    nextLbxSearchSongsSelectedIndex = lbxSearchSongs.SelectedIndex;
                    await viewModel.SongSearch.AddSongsToSearchPlaylist(new Song[] { addSong }, addType);
                    viewModel.PlayState = PlaybackState.Playing;
                }

                break;

            case Key.Escape:
                viewModel.SongSearch.SearchKey = string.Empty;
                break;

            case Key.Up:
                if (lbxSearchSongs.Items.Count > 0 && viewModel.SongSearch.IsSearching)
                {
                    lbxSearchSongs.SelectedIndex =
                        StdUtils.OffsetIndex(lbxSearchSongs.SelectedIndex, lbxSearchSongs.Items.Count, -1).index;
                }

                break;

            case Key.Down:
                if (lbxSearchSongs.Items.Count > 0 && viewModel.SongSearch.IsSearching)
                {
                    lbxSearchSongs.SelectedIndex =
                        StdUtils.OffsetIndex(lbxSearchSongs.SelectedIndex, lbxSearchSongs.Items.Count, 1).index;
                }

                break;

            default:
                e.Handled = false;
                break;
        }
    }

    private void TbxSearch_GotFocus(object sender, RoutedEventArgs e)
    {
        viewModel?.SongSearch.Start();
    }

    private void TbxSearch_LostFocus(object sender, RoutedEventArgs e)
    {
        viewModel?.SongSearch.Stop();
    }

    private void MimReloadSongs_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as StyledElement)?.DataContext is PlaylistInfo playlistInfo)
        {
            IUpdateLibraryService? updateLibraryService = audioServices?.GetUpdateLibraryService();
            updateLibraryService?.ReloadSourcePlaylist(playlistInfo.Id);
        }
    }

    private void MimRemixSongs_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as StyledElement)?.DataContext is PlaylistInfo playlistInfo)
        {
            viewModel?.RemixSongs(playlistInfo.Id);
        }
    }

    private void MimRemovePlaylist_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as StyledElement)?.DataContext is PlaylistInfo playlistInfo)
        {
            viewModel?.RemovePlaylist(playlistInfo.Id);
        }
    }

    private async void BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (await UpdateBuildConfig())
        {
            await audioServicesHandler.Stop();
            audioServicesHandler.Start(servicesBuildConfig);
        }
    }

    private void BtnAddPlaylist_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F3
            || e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            tbxSearch.Focus();
        }
    }

    private void SldPosition_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        Slider slider = (Slider)sender!;
        double positionSeconds = e.NewValue;
        double durationSeconds = slider.Maximum;
        IPlaylistViewModel? currentPlaylist = viewModel?.CurrentPlaylist;

        if (currentPlaylist is { Id: not null }
            && currentPlaylist.CurrentSongRequest.TryHasValue(out SongRequest request)
            && Math.Abs(request.Duration.TotalSeconds - durationSeconds) < 0.01
            && Math.Abs(request.Position.TotalSeconds - positionSeconds) > 0.01)
        {
            currentPlaylist.SetCurrentSongRequest(SongRequest.Get(request.Id, TimeSpan.FromSeconds(positionSeconds),
                request.Duration));
        }
    }

    private void OnPrevious(object? sender, RoutedEventArgs e)
    {
        viewModel?.CurrentPlaylist.SetPreviousSong();
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        viewModel?.CurrentPlaylist.SetNextSong();
    }

    private void StpCurrentSong_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Scroll();
    }

    private void Scroll()
    {
        if (lbxSongs.SelectedItem != null) lbxSongs.ScrollIntoView(lbxSongs.SelectedItem);
        else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]!);
    }
}