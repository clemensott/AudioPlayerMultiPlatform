using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public interface ILibraryRepoService
    {
        IEnumerable<IServicedLibraryRepo> GetRepos();

        void AddRepo(IServicedLibraryRepo repo);
        
        void RemoveRepo(IServicedLibraryRepo repo);
    }
}
