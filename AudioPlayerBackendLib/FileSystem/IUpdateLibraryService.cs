using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    public interface IUpdateLibraryService : IAudioService
    {
        event EventHandler UpdateStarted;
        event EventHandler UpdateCompleted;

        Task ReloadLibrary();

        Task UpdateLibrary();
        
        Task UpdatePlaylists();

        Task ReloadSourcePlaylist(Guid id);

        Task<Song[]> LoadSongs(FileMediaSources fileMediaSources);

        Task UpdateSourcePlaylist(Guid id);
    }
}
