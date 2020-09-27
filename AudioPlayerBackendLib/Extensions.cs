using AudioPlayerBackend.Audio;
using System.Collections.Generic;

namespace AudioPlayerBackend
{
    public static class Extensions
    {
        public static IEnumerable<IPlaylistBase> GetAllPlaylists(this IAudioServiceBase service)
        {
            foreach (ISourcePlaylist playlist in service.SourcePlaylists) yield return playlist;
            foreach (IPlaylist playlist in service.Playlists) yield return playlist;
        }

        public static IEnumerable<IPlaylist> GetAllPlaylists(this IAudioService service)
        {
            foreach (ISourcePlaylist playlist in service.SourcePlaylists) yield return playlist;
            foreach (IPlaylist playlist in service.Playlists) yield return playlist;
        }
    }
}
