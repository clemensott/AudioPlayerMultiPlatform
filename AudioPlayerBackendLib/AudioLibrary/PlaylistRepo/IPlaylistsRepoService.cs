using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepoService
    {
        IEnumerable<IServicedPlaylistsRepo> GetRepos();

        void AddRepo(IServicedPlaylistsRepo repo);

        void RemoveRepo(IServicedPlaylistsRepo repo);
    }
}
