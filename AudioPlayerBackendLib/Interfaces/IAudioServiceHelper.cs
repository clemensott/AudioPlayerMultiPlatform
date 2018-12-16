using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;

namespace AudioPlayerBackend
{
    public interface IAudioServiceHelper : IAudioClientHelper
    {
        IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service);

        Action<IAudioService> SetCurrentSongThreadSafe { get; }

        IEnumerable<string> LoadFilePaths(string path, IAudioService service);
    }
}
