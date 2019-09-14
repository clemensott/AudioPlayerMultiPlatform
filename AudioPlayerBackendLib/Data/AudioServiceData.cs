using AudioPlayerBackend.Audio;
using StdOttStandard;
using System.Linq;

namespace AudioPlayerBackend.Data
{
    public class AudioServiceData
    {
        public int CurrentPlaylistIndex { get; set; }

        public PlaylistData SourcePlaylist { get; set; }

        public PlaylistData[] Playlists { get; set; }

        public float Volume { get; set; }

        public AudioServiceData() { }

        public AudioServiceData(IAudioServiceBase service)
        {
            SourcePlaylist = new PlaylistData(service.SourcePlaylist);
            Playlists = service.Playlists.Select(p => new PlaylistData(p)).ToArray();
            CurrentPlaylistIndex = service.Playlists.IndexOf(service.CurrentPlaylist);
            Volume = service.Volume;
        }
    }
}
