using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Converter.MultipleInputs;
using StdOttStandard.Linq;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für AddSourcePlaylistWindow.xaml
    /// </summary>
    public partial class AddSourcePlaylistWindow : Window
    {
        private readonly IFileSystemService fileSystemService;
        private readonly IAudioService service;
        private readonly ISourcePlaylist newPlaylist;

        public AddSourcePlaylistWindow(string[] sources, IAudioService service)
        {
            InitializeComponent();

            fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();
            this.service = service;
            IAudioCreateService audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
            newPlaylist = audioCreateService.CreateSourcePlaylist(Guid.NewGuid());
            newPlaylist.Loop = LoopType.CurrentPlaylist;

            try
            {
                newPlaylist.Name = sources.Length == 1 ?
                    Path.GetFileNameWithoutExtension(sources[0]) :
                    Path.GetFileName(Path.GetDirectoryName(sources[0]));
            }
            catch { }

            DataContext = service;
            gidNewPlaylist.DataContext = newPlaylist;
            tbxSources.Text = sources.Join();
        }

        private object MicNewPlaylist_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            if (args.Input0 == null || args.Input1 == null) return false;

            int count = (int)args.Input0;
            bool isNewPlaylistChecked = (bool?)args.Input1 == true;

            return count == 0 || isNewPlaylistChecked;
        }

        private object MicOk_Convert(object sender, MultiplesInputsConvert3EventArgs args)
        {
            if (args.Input1 == null) return false;

            ISourcePlaylist selectedPlaylist = (ISourcePlaylist)args.Input0;
            bool isNewPlaylist = (bool)args.Input1;
            string newPlaylistName = (string)args.Input2;

            return isNewPlaylist ? !string.IsNullOrWhiteSpace(newPlaylistName) : selectedPlaylist != null;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string[] paths = tbxSources.Text?.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();
            var fileMediaSourceRoots = service.FileMediaSourceRoots.ToNotNull();
            (IList<FileMediaSource> newSoruces, IList<FileMediaSourceRoot> newRoots) =
                FileMediaSourcesHelper.ExtractFileMediaSources(paths, fileMediaSourceRoots);

            if ((bool)micNewPlaylist.Output)
            {
                FileMediaSourceRoot[] newAllRoots = fileMediaSourceRoots.ToNotNull().Concat(newRoots).ToArray();
                newPlaylist.FileMediaSources = newSoruces.ToArray();
                fileSystemService.UpdateSourcePlaylist(newPlaylist, newAllRoots);
                service.SourcePlaylists.Add(newPlaylist);
                service.CurrentPlaylist = newPlaylist;
                service.FileMediaSourceRoots = newAllRoots;
            }
            else
            {
                ISourcePlaylist selectedPlaylist = (ISourcePlaylist)lbxPlaylists.SelectedItem;
                if (rbnAppend.IsChecked == true)
                {
                    selectedPlaylist.FileMediaSources = selectedPlaylist.FileMediaSources.Concat(newSoruces).ToArray();
                    service.FileMediaSourceRoots = fileMediaSourceRoots.Concat(newRoots).ToArray();
                }
                else
                {
                    selectedPlaylist.FileMediaSources = newSoruces.ToArray();
                    service.FileMediaSourceRoots = fileMediaSourceRoots
                        .Where(root => service.SourcePlaylists.SelectMany(p => p.FileMediaSources).Any(s => s.RootId == root.Id))
                        .ToArray();
                }

                fileSystemService.UpdateSourcePlaylist(selectedPlaylist, service.FileMediaSourceRoots);
            }

            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
