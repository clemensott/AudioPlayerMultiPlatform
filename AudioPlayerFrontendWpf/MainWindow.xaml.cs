using AudioPlayerBackendLib;
using AudioPlayerFrontendWpf.ViewModel;
using StdOttWpfLib.Hotkey;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AudioPlayerFrontendWpf
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string hotKeyFileName = "hotkeys.txt",
            previousKey = "Previous=", playPauseKey = "PlayPause=", nextKey = "Next=";

        private ViewModelBase viewModel;
        private HotKey[] hotKeys;
        private WidthService widthService;

        public MainWindow()
        {
            InitializeComponent();

            DependentViewModel dvm = new DependentViewModel();
            mainGrid.Children.Add(dvm.Service.GetMediaElement());

            DataContext = viewModel = dvm;

            viewModel.Sources = Environment.GetCommandLineArgs().Skip(1).ToArray();
            viewModel.IsAllShuffle = true;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentPlaySong":
                    widthService.Reset();
                    ScollToCurrentShowSong();
                    break;

                case "Songs":
                    ScollToCurrentShowSong();
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string directory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                string path = Path.Combine(directory, hotKeyFileName);
                string[] hotKeyLines = File.ReadAllLines(path);

                hotKeys = HotKey.GetRegisteredHotKeys(hotKeyLines, GetHotKeySources()).ToArray();
            }
            catch (Exception exc)
            {
                string message = string.Format("{0} while loading main window:\n{1}", exc.GetType().Name, exc.Message);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            viewModel.PlayState = PlayState.Play;

            widthService = new WidthService(tblTitle, tblArtist, cdSong, cdSlider);
            widthService.Reset();

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private IEnumerable<HotKeySource> GetHotKeySources()
        {
            yield return new HotKeySource(previousKey, Previous_Pressed);
            yield return new HotKeySource(playPauseKey, PlayPause_Pressed);
            yield return new HotKeySource(nextKey, Next_Pressed);
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
                    if (viewModel.Songs.Any())
                    {
                        viewModel.CurrentPlaySong = viewModel.Songs.First();
                        viewModel.PlayState = PlayState.Play;
                    }
                    break;

                case Key.Escape:
                    viewModel.SearchKey = string.Empty;
                    break;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Refresh();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            widthService?.Update();
        }

        private void ScollToCurrentShowSong()
        {
            if (viewModel.CurrentViewSong != null) lbxSongs.ScrollIntoView(viewModel.CurrentViewSong);
        }

        private void Previous_Pressed(object sender, KeyPressedEventArgs e) => Previous();

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SetPreviousSong();
        }

        private void Previous()
        {
            viewModel.SetPreviousSong();

            ScollToCurrentShowSong();
        }

        private void PlayPause_Pressed(object sender, KeyPressedEventArgs e) => TogglePlayPause();

        private void TogglePlayPause()
        {
            viewModel.PlayState = viewModel.PlayState == PlayState.Play ? PlayState.Pause : PlayState.Play;
        }

        private void Next_Pressed(object sender, KeyPressedEventArgs e) => Next();

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SetNextSong();
        }

        private void Next()
        {
            viewModel.SetNextSong();

            ScollToCurrentShowSong();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                viewModel.Sources = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (HotKey hotKey in hotKeys ?? Enumerable.Empty<HotKey>()) hotKey?.Dispose();
        }
    }
}
