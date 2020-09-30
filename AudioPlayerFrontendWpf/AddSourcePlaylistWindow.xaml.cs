using System.IO;
using System.Linq;
using System.Windows;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerFrontend.Join;
using StdOttStandard.Converter.MultipleInputs;
using StdOttStandard.Linq;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für AddSourcePlaylistWindow.xaml
    /// </summary>
    public partial class AddSourcePlaylistWindow : Window
    {
        private readonly IAudioService service;
        private readonly ISourcePlaylist newPlaylist;

        public AddSourcePlaylistWindow(string[] sources, IAudioService service)
        {
            InitializeComponent();

            this.service = service;
            newPlaylist = (ISourcePlaylist)service.CreateSourcePlaylist();
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
            string[] sources = tbxSources.Text?.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

            if ((bool)micNewPlaylist.Output)
            {
                newPlaylist.FileMediaSources = sources;
                service.SourcePlaylists.Add(newPlaylist);
                service.CurrentPlaylist = newPlaylist;
            }
            else
            {
                ISourcePlaylist selectedPlaylist = (ISourcePlaylist)lbxPlaylists.SelectedItem;
                selectedPlaylist.FileMediaSources = rbnAppend.IsChecked == true ?
                    selectedPlaylist.FileMediaSources.ToNotNull().Concat(sources).ToArray() : sources;
            }

            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
