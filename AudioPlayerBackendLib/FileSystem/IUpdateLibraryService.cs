using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    public interface IUpdateLibraryService
    {
        Task ReloadLibrary();

        Task UpdateLibrary();

        Task ReloadSourcePlaylist(Guid id);

        Task<Song[]> ReloadSourcePlaylist(FileMediaSources fileMediaSources);

        Task UpdateSourcePlaylist(Guid id);
    }
}
