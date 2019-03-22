using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerFrontend.Join
{
    class PlayerHelper : IAudioServicePlayerHelper, IAudioStreamHelper
    {
        private static PlayerHelper instance;

        public static PlayerHelper Current
        {
            get
            {
                if (instance == null) instance = new PlayerHelper();

                return instance;
            }
        }

        private PlayerHelper() { }

        public Action<IServicePlayer> SetCurrentSongThreadSafe => null;

        public Action<Action> InvokeDispatcher => null;

        public IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format, IAudioServiceBase service)
        {
            return new BufferedWaveProvider(format);
        }

        public IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service)
        {
            return new AudioFileReader(song.FullPath);
        }
    }
}
