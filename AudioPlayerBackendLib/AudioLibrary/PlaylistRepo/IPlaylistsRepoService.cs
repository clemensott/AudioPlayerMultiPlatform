using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepoService
    {
        void ForEachRepoExcept(Action<IServicedPlaylistsRepo> action, IServicedPlaylistsRepo repo);

        void AddRepo(IServicedPlaylistsRepo repo);

        void RemoveRepo(IServicedPlaylistsRepo repo);
    }
}
