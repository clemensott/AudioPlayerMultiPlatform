using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using AudioPlayerFrontend.Join;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum OpenState { Open, TryOpening, IDLE, Settings }

        private OpenState openState;
        private Exception buildException;
        private ServiceBuilder serviceBuilder;
        private HotKeysBuilder hotKeysBuilder;
        private ViewModel viewModel;
        private HotKeys hotKeys;
        private WidthService widthService;

        public MainWindow()
        {
            InitializeComponent();

            widthService = new WidthService(tblTitle, tblArtist, cdSong, sldPosition);

            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            dpd.AddValueChanged(lbxSongs, OnItemsSourceChanged);

            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void OnItemsSourceChanged(object sender, EventArgs e)
        {
            Scroll();
        }

        private async void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            if (viewModel?.Base is IMqttAudio audio)
            {
                switch (e.Mode)
                {
                    case PowerModes.Resume:
                        try
                        {
                            SetTryOpeningVisibilities();

                            await OpenAsync(audio);
                            Subscribe(hotKeys);

                            SetOpenVisibilities();
                        }
                        catch
                        {
                            if (openState == OpenState.Settings) await UpdateBuildersAndBuild();
                            else SetWaitVisibilities();
                        }
                        break;

                    case PowerModes.Suspend:
                        Unsubscribe(hotKeys);
                        await audio.CloseAsync();
                        break;
                }
            }
        }

        private async Task OpenAsync(IMqttAudio audio)
        {
            while (true)
            {
                try
                {
                    await audio.OpenAsync();
                    break;
                }
                catch (Exception exc)
                {
                    await Task.Delay(500);

                    if (openState == OpenState.TryOpening) continue;

                    MessageBox.Show(exc.ToString(), "Open service", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<string> args = Environment.GetCommandLineArgs().Skip(1);
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            serviceBuilder = new ServiceBuilder(new ServiceBuilderHelper())
                .WithArgs(args)
                .WithPlayer(new Join.Player(-1, windowHandle));
            hotKeysBuilder = new HotKeysBuilder().WithArgs(args);

            await Build();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3) tbxSearch.Focus();
        }

        private void TbxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (viewModel.CurrentPlaylist.SearchSongs.Any())
                    {
                        viewModel.CurrentPlaylist.CurrentSong = viewModel.CurrentPlaylist.SearchSongs.First();
                        viewModel.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    viewModel.CurrentPlaylist.SearchKey = string.Empty;
                    break;
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Reload();
        }

        private void OnPrevious(object sender, EventArgs e)
        {
            viewModel.SetPreviousSong();
        }

        private void OnTogglePlayPause(object sender, EventArgs e)
        {
            viewModel.PlayState = viewModel.PlayState == PlaybackState.Playing ?
                PlaybackState.Paused : PlaybackState.Playing;
        }

        private void OnNext(object sender, EventArgs e)
        {
            viewModel.SetNextSong();
        }

        private void OnPlay(object sender, EventArgs e)
        {
            viewModel.PlayState = PlaybackState.Playing;
        }

        private void OnPause(object sender, EventArgs e)
        {
            viewModel.PlayState = PlaybackState.Paused;
        }

        private void OnRestart(object sender, EventArgs e)
        {
            viewModel.CurrentPlaylist.Position = TimeSpan.Zero;
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            await UpdateBuildersAndBuild();
        }

        private async Task UpdateBuildersAndBuild()
        {
            UpdateBuilders();

            await Build();
        }

        private async Task Build()
        {
            try
            {
                SetTryOpeningVisibilities();

                (IAudioExtended audio, HotKeys hotKeys) = await Build(serviceBuilder, hotKeysBuilder);

                if (audio != viewModel?.Base)
                {
                    viewModel?.Dispose();

                    DataContext = viewModel = new ViewModel(audio);
                }

                if (hotKeys != this.hotKeys)
                {
                    Unsubscribe(this.hotKeys);
                    this.hotKeys = hotKeys;
                    Subscribe(hotKeys);
                }

                SetOpenVisibilities();
            }
            catch
            {
                if (openState == OpenState.Settings) await UpdateBuildersAndBuild();
                else SetWaitVisibilities();
            }
        }

        private void UpdateBuilders()
        {
            if (viewModel?.Base != null) serviceBuilder.WithService(viewModel.Base);
            if (hotKeys != null) hotKeysBuilder.WithHotKeys(hotKeys);

            SettingsWindow window = new SettingsWindow(serviceBuilder, hotKeysBuilder);
            window.ShowDialog();
        }

        private async Task<(IAudioExtended audio, HotKeys hotKeys)> Build(ServiceBuilder serviceBuilder, HotKeysBuilder hotKeysBuilder)
        {
            IAudioExtended audio;
            HotKeys hotKeys;

            while (true)
            {
                try
                {
                    audio = await serviceBuilder.Build();
                    break;
                }
                catch (Exception exc)
                {
                    buildException = exc;
                    btnOpeningException.Visibility = Visibility.Visible;

                    await Task.Delay(500);

                    if (openState == OpenState.TryOpening) continue;
                    else throw;
                }
            }

            try
            {
                hotKeys = hotKeysBuilder.Build();
                Subscribe(hotKeys);
            }
            catch (Exception exc)
            {
                buildException = exc;
                btnOpeningException.Visibility = Visibility.Visible;
                throw;
            }

            return (audio, hotKeys);
        }

        private void LbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scroll();
        }

        private void Scroll()
        {
            if (lbxSongs.SelectedItem != null) lbxSongs.ScrollIntoView(lbxSongs.SelectedItem);
            else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                viewModel.FileMediaSources = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            hotKeys?.Dispose();

            viewModel?.Dispose();
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Playlist playlist = new Playlist()
            {
                Songs = viewModel.FileBasePlaylist.ViewSongs.Take(3).ToArray(),
                SearchKey = "Hi",
                CurrentSong = viewModel.FileBasePlaylist.ViewSongs.ElementAtOrDefault(1),
                IsAllShuffle = true,
                Loop = LoopType.Next
            };

            viewModel.Base.AdditionalPlaylists.Add(playlist);
            viewModel.CurrentPlaylist = viewModel.AdditionalPlaylists.LastOrDefault();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            openState = OpenState.IDLE;
        }

        private void BtnOpeningSettings_Click(object sender, RoutedEventArgs e)
        {
            openState = OpenState.Settings;
        }

        private async void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            await Build();
        }

        private void BtnException_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(buildException.ToString(), "Building Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SetTryOpeningVisibilities()
        {
            openState = OpenState.TryOpening;
            buildException = null;

            stpOpening.Visibility = Visibility.Visible;

            wrpSettings.Visibility = Visibility.Collapsed;
            lbxSongs.Visibility = Visibility.Collapsed;
            grdControl.Visibility = Visibility.Collapsed;
            stpWaitRetry.Visibility = Visibility.Collapsed;
        }

        private void SetOpenVisibilities()
        {
            openState = OpenState.Open;

            stpOpening.Visibility = Visibility.Collapsed;
            stpWaitRetry.Visibility = Visibility.Collapsed;

            wrpSettings.Visibility = Visibility.Visible;
            lbxSongs.Visibility = Visibility.Visible;
            grdControl.Visibility = Visibility.Visible;
        }

        private void SetWaitVisibilities()
        {
            openState = OpenState.IDLE;

            stpWaitRetry.Visibility = Visibility.Visible;
            btnWaitException.Visibility = buildException != null ? Visibility.Visible : Visibility.Collapsed;

            wrpSettings.Visibility = Visibility.Collapsed;
            lbxSongs.Visibility = Visibility.Collapsed;
            grdControl.Visibility = Visibility.Collapsed;
            stpOpening.Visibility = Visibility.Collapsed;
        }

        private void Subscribe(HotKeys hotKeys)
        {
            if (hotKeys == null) return;

            hotKeys.Toggle_Pressed += OnTogglePlayPause;
            hotKeys.Next_Pressed += OnNext;
            hotKeys.Previous_Pressed += OnPrevious;
            hotKeys.Play_Pressed += OnPlay;
            hotKeys.Pause_Pressed += OnPause;
            hotKeys.Restart_Pressed += OnRestart;

            hotKeys.Register();
        }

        private void Unsubscribe(HotKeys hotKeys)
        {
            if (hotKeys == null) return;

            hotKeys.Unregister();

            hotKeys.Toggle_Pressed -= OnTogglePlayPause;
            hotKeys.Next_Pressed -= OnNext;
            hotKeys.Previous_Pressed -= OnPrevious;
            hotKeys.Play_Pressed -= OnPlay;
            hotKeys.Pause_Pressed -= OnPause;
            hotKeys.Restart_Pressed -= OnRestart;
        }
    }
}
