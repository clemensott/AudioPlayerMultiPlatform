using System.Linq;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;

namespace AudioPlayerBackend.Data
{
    public class SourcePlaylistData : PlaylistData
    {
        public FileMediaSource[] Sources { get; set; }

        public SourcePlaylistData() { }

        public SourcePlaylistData(ISourcePlaylistBase playlist) : base(playlist)
        {
            Sources = playlist.FileMediaSources.ToArray();
        }
    }
}
