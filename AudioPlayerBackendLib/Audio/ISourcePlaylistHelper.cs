using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Audio
{
    public interface ISourcePlaylistHelper
    {
        IInvokeDispatcherHelper Dispatcher { get; }

        Func<string[], Task<IEnumerable<string>>> FetchFiles { get; }

        Func<string, Task<Song?>> CreateSong { get; }
    }
}
