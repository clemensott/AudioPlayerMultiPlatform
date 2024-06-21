using System;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public interface ILibraryRepoService
    {
        void ForEachRepoExcept(Action<IServicedLibraryRepo> action, IServicedLibraryRepo repo);

        void AddRepo(IServicedLibraryRepo repo);
        
        void RemoveRepo(IServicedLibraryRepo repo);
    }
}
