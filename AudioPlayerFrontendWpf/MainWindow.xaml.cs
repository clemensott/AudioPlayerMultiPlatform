using AudioPlayerBackendLib;
using NAudio.Wave;
using StdOttWpfLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AudioPlayerFrontendWpf
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string hotKeyFileName = "hotkeys.txt",
            previousKey = "Previous=", playPauseKey = "PlayPause=", nextKey = "Next=";

        private ViewModel viewModel;
        private HotKeys hotKeys;
        private WidthService widthService;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                IEnumerable<string> args = Environment.GetCommandLineArgs().Skip(1);
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                ServiceBuilder serviceBuilder = new ServiceBuilder()
                    .WithArgs(args)
                    .WithWindowHandler(windowHandle);

                viewModel = new ViewModel(await serviceBuilder.Build());
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                DataContext = viewModel;
            }
            catch (Exception exc)
            {
                MessageBox.Show(Utils.Convert(exc), "Building service", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            try
            {
                IEnumerable<string> args = Environment.GetCommandLineArgs().Skip(1);

                hotKeys = new HotKeysBuilder().WithArgs(args).Build();

                hotKeys.Toggle_Pressed += OnTogglePlayPause;
                hotKeys.Next_Pressed += OnNext;
                hotKeys.Previous_Pressed += OnPrevious;
                hotKeys.Play_Pressed += OnPlay;
                hotKeys.Pause_Pressed += OnPause;
                hotKeys.Restart_Pressed += OnRestart;

                hotKeys.Register();
            }
            catch (Exception exc)
            {
                MessageBox.Show(Utils.Convert(exc), "Building hotkeys", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            widthService = new WidthService(tblTitle, tblArtist, cdSong, cdSlider);
            widthService.Reset();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.CurrentSong):
                    widthService.Reset();

                    if (lbxSongs.Items.Contains(viewModel.CurrentSong)) lbxSongs.ScrollIntoView(viewModel.CurrentSong);
                    break;

                case nameof(viewModel.ViewSongs):
                    if (!viewModel.CurrentSong.HasValue) break;

                    if (lbxSongs.Items.Contains(viewModel.CurrentSong.Value)) lbxSongs.ScrollIntoView(viewModel.CurrentSong.Value);
                    else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]);
                    break;
            }
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
                    if (viewModel.ViewSongs.Any())
                    {
                        viewModel.CurrentSong = viewModel.ViewSongs.First();
                        viewModel.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    viewModel.SearchKey = string.Empty;
                    break;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Reload();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            widthService?.Update();
        }

        private void OnPrevious(object sender, EventArgs e)
        {
            Previous();
        }

        private void Previous()
        {
            viewModel.SetPreviousSong();
        }

        private void OnTogglePlayPause(object sender, EventArgs e) => TogglePlayPause();

        private void TogglePlayPause()
        {
            viewModel.PlayState = viewModel.PlayState == PlaybackState.Playing ? PlaybackState.Paused : PlaybackState.Playing;
        }

        private void OnNext(object sender, EventArgs e)
        {
            Next();
        }

        private void Next()
        {
            viewModel.SetNextSong();
        }

        private void OnPlay(object sender, EventArgs e)
        {
            Play();
        }

        private void Play()
        {
            viewModel.PlayState = PlaybackState.Playing;
        }

        private void OnPause(object sender, EventArgs e)
        {
            Pause();
        }

        private void Pause()
        {
            viewModel.PlayState = PlaybackState.Paused;
        }

        private void OnRestart(object sender, EventArgs e)
        {
            Restart();
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow(viewModel.Parent, hotKeys);
            window.ShowDialog();

            IAudioExtended service = await window.ServiceBuilder.Build();

            if (service != viewModel.Parent)
            {
                viewModel.Dispose();
                DataContext = viewModel = new ViewModel(service);
            }
        }

        private void Restart()
        {
            viewModel.Position = TimeSpan.Zero;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                viewModel.MediaSources = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            hotKeys?.Dispose();

            viewModel.Dispose();
        }
    }
}
