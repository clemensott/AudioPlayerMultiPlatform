using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using AudioPlayerFrontend.Join;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private ViewModel viewModel;
        private HotKeys hotKeys;
        private WidthService widthService;

        public MainWindow()
        {
            InitializeComponent();

            widthService = new WidthService(tblTitle, tblArtist, cdSong, sldPosition);

            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            dpd.AddValueChanged(lbxSongs, OnItemsSourceChanged);
        }

        private void OnItemsSourceChanged(object sender, EventArgs e)
        {
            Scroll();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                IEnumerable<string> args = Environment.GetCommandLineArgs().Skip(1);
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                ServiceBuilder serviceBuilder = new ServiceBuilder(new ServiceBuilderHelper())
                    .WithArgs(args)
                    .WithPlayer(new Join.Player(-1, windowHandle));

                DataContext = viewModel = new ViewModel(await serviceBuilder.Build());
            }
            catch (Exception exc)
            {
                MessageBox.Show(Utils.GetTypeMessageAndStack(exc), "Building service", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(Utils.GetTypeMessageAndStack(exc), "Building hotkeys", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            viewModel.Position = TimeSpan.Zero;
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

            hotKeys = window.HotKeysBuilder.Build();
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
