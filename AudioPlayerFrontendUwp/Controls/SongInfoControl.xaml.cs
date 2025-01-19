using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace AudioPlayerFrontend.Controls
{
    public sealed partial class SongInfoControl : UserControl
    {
        public SongInfoControl()
        {
            this.InitializeComponent();
        }

        public async Task LoadData(Song song)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(song.FullPath);
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            tblPath.Text = file.Path;
            tblTitle.Text = properties.Title;
            tblArtist.Text = properties.Artist;
            tblAlbum.Text = properties.Album;
            tblYear.Text = properties.Year.ToString();
            tblDuration.Text = properties.Duration.ToString("c");
            tblBitrate.Text = $"{properties.Bitrate / 1024} kBit/s";
        }
    }
}
