using System;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepoService
    {
        void ForEachRepoExcept(Action<IServicedPlaylistRepo> action, IServicedPlaylistRepo repo);

        void AddRepo(IServicedPlaylistRepo repo);

        void RemoveRepo(IServicedPlaylistRepo repo);
    }
}
