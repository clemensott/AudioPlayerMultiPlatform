using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions
{
    public static class PlaylistsExtensions
    {
        public static PlaylistInfo ToPlaylistInfo(this Playlist playlist)
        {
            return new PlaylistInfo(playlist.Id, playlist.Type, playlist.Name, playlist.Songs.Count,
                playlist.FilesLastUpdated, playlist.SongsLastUpdated);
        }

        public static IEnumerable<PlaylistInfo> GetSourcePlaylists(this IEnumerable<PlaylistInfo> playlists)
        {
            return playlists.Where(p => p.Type.HasFlag(PlaylistType.SourcePlaylist));
        }
    }
}
