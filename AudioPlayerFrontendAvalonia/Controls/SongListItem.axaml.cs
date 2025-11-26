using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using Avalonia;
using Avalonia.Controls;

namespace AudioPlayerFrontendAvalonia.Controls;

public partial class SongListItem : UserControl
{
    public static readonly StyledProperty<Song?> SongProperty = AvaloniaProperty
        .Register<SongListItem, Song?>(nameof(Song));

    static SongListItem()
    {
        SongProperty.Changed.AddClassHandler<SongListItem>(SongChanged);
    }

    private static void SongChanged(SongListItem sender, AvaloniaPropertyChangedEventArgs e)
    {
        Song? song = (Song?)e.NewValue;
        sender.tblIndex.Text = song?.Index.ToString() ?? string.Empty;
        sender.tblTitle.Text = song?.Title ?? string.Empty;
        sender.tblArtist.Text = song?.Artist ?? "<Unknown>";
    }

    public Song? Song
    {
        get => GetValue(SongProperty);
        set => SetValue(SongProperty, value);
    }

    public SongListItem()
    {
        this.InitializeComponent();
    }
}