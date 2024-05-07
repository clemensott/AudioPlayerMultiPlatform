using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
using System.Linq;

namespace AudioPlayerBackend.Data
{
    public class AudioServiceData
    {
        public string CurrentPlaylistID { get; set; }

        public FileMediaSourceRoot[] FileMediaSourceRoots { get; set; }

        public SourcePlaylistData[] SourcePlaylists { get; set; }

        public PlaylistData[] Playlists { get; set; }

        public float Volume { get; set; }

        public AudioServiceData() { }

        public AudioServiceData(IAudioServiceBase service)
        {
            SourcePlaylists = service.SourcePlaylists.Select(p => new SourcePlaylistData(p)).ToArray();
            Playlists = service.Playlists.Select(p => new PlaylistData(p)).ToArray();
            CurrentPlaylistID = service.CurrentPlaylist?.ID.ToString();
            FileMediaSourceRoots = service.FileMediaSourceRoots;
            Volume = service.Volume;
        }
    }
}
