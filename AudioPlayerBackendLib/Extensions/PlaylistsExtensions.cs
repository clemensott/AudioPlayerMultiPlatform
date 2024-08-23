using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.Extensions
{
    public static class PlaylistsExtensions
    {
        public static PlaylistInfo ToPlaylistInfo(this Playlist playlist)
        {
            return new PlaylistInfo(playlist.Id, playlist.Type, playlist.Name, playlist.Songs.Count);
        }

        public static IEnumerable<PlaylistInfo> GetSourcePlaylists(this IEnumerable<PlaylistInfo> playlists)
        {
            return playlists.Where(p => p.Type.HasFlag(PlaylistType.SourcePlaylist));
        }
    }
}
