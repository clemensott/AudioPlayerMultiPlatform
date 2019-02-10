using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using AudioPlayerFrontend.Join;
using Microsoft.Win32;
using StdOttStandard.CommendlinePaser;
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
        private ServiceBuilder serviceBuilder;
        private HotKeysBuilder hotKeysBuilder;
        private ViewModel viewModel;
        private HotKeys hotKeys;
        private WidthService widthService;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = viewModel = new ViewModel();
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
            if (viewModel.AudioService?.Base is IMqttAudio audio)
            {
                switch (e.Mode)
                {
                    case PowerModes.Resume:
                        try
                        {
                            viewModel.AudioServiceState = OpenState.TryOpening;

                            await OpenAsync(audio);
                            Subscribe(hotKeys);

                            viewModel.AudioServiceState = OpenState.Open;
                        }
                        catch
                        {
                            if (viewModel.AudioServiceState == OpenState.Settings) await UpdateBuildersAndBuild();
                            else viewModel.AudioServiceState = OpenState.IDLE;
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

                    if (viewModel.AudioServiceState == OpenState.TryOpening) continue;

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

            Option disableUiOpt = Option.GetLongOnly("disable-ui", "Disables UI on startup.", false, 0);
            OptionParseResult result = new Options(disableUiOpt).Parse(args);

            if (result.TryGetFirstValidOptionParseds(disableUiOpt, out _)) viewModel.IsUiEnabled = false;

            await Build();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3) tbxSearch.Focus();
        }

        private void TbxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            AudioViewModel audio = viewModel.AudioService;

            if (audio == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    if (audio.CurrentPlaylist.SearchSongs.Any())
                    {
                        audio.CurrentPlaylist.CurrentSong = audio.CurrentPlaylist.SearchSongs.First();
                        audio.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    audio.CurrentPlaylist.SearchKey = string.Empty;
                    break;
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.Reload();
        }

        private void OnPrevious(object sender, EventArgs e)
        {
            viewModel.AudioService?.SetPreviousSong();
        }

        private void OnTogglePlayPause(object sender, EventArgs e)
        {
            AudioViewModel audio = viewModel.AudioService;

            if (audio == null) return;

            audio.PlayState = audio.PlayState == PlaybackState.Playing ?
                PlaybackState.Paused : PlaybackState.Playing;
        }

        private void OnNext(object sender, EventArgs e)
        {
            viewModel.AudioService?.SetNextSong();
        }

        private void OnPlay(object sender, EventArgs e)
        {
            if (viewModel.AudioService != null) viewModel.AudioService.PlayState = PlaybackState.Playing;
        }

        private void OnPause(object sender, EventArgs e)
        {
            if (viewModel.AudioService != null) viewModel.AudioService.PlayState = PlaybackState.Paused;
        }

        private void OnRestart(object sender, EventArgs e)
        {
            if (viewModel.AudioService != null) viewModel.AudioService.CurrentPlaylist.Position = TimeSpan.Zero;
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
                viewModel.AudioServiceState = OpenState.TryOpening;

                (IAudioExtended audio, HotKeys hotKeys) = await Build(serviceBuilder, hotKeysBuilder);

                if (audio != viewModel.AudioService?.Base)
                {
                    viewModel.AudioService?.Dispose();

                    viewModel.AudioService = new AudioViewModel(audio);
                }

                if (hotKeys != this.hotKeys)
                {
                    Unsubscribe(this.hotKeys);
                    this.hotKeys = hotKeys;
                    Subscribe(hotKeys);
                }

                viewModel.AudioServiceState = OpenState.Open;
            }
            catch
            {
                if (viewModel.AudioServiceState == OpenState.Settings) await UpdateBuildersAndBuild();
                else viewModel.AudioServiceState = OpenState.IDLE;
            }
        }

        private void UpdateBuilders()
        {
            if (viewModel.AudioService?.Base != null) serviceBuilder.WithService(viewModel.AudioService.Base);
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
                    viewModel.BuildException = exc;
                    btnOpeningException.Visibility = Visibility.Visible;

                    await Task.Delay(500);

                    if (viewModel.AudioServiceState == OpenState.TryOpening) continue;
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
                viewModel.BuildException = exc;
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
            if (viewModel.AudioService != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                viewModel.AudioService.FileMediaSources = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            hotKeys?.Dispose();

            viewModel.AudioService?.Dispose();
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioServiceState = OpenState.IDLE;
        }

        private void BtnOpeningSettings_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioServiceState = OpenState.Settings;
        }

        private async void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            await Build();
        }

        private void BtnException_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(viewModel.BuildException.ToString(), "Building Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void StpCurrentSong_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Scroll();
        }
    }
}
