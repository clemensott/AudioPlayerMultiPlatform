using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;

namespace AudioPlayerBackend.Extensions
{
    public static class PlaylistsExtensions
    {
        public static PlaylistInfo ToPlaylistInfo(this Playlist playlist)
        {
            return new PlaylistInfo(playlist.Id, playlist.Type, playlist.Name, playlist.Songs.Count);
        }
    }
}
