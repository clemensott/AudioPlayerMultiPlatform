using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    class LibraryRepoService: RepoService<IServicedLibraryRepo>, ILibraryRepoService
    {
    }
}
