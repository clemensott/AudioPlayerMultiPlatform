using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.IO;

namespace AudioPlayerFrontend.Join
{
    class AudioHelper : IMqttAudioClientHelper, IMqttAudioServiceHelper
    {
        public Action<IAudioService> SetCurrentSongThreadSafe => null;

        public Action<Action> InvokeDispatcher => null;

        public IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format, IMqttAudioClient client)
        {
            return new BufferedWaveProvider(format);
        }

        public IMqttClient CreateMqttClient(IMqttAudioClient client)
        {
            return new MqttClient(new MqttFactory().CreateMqttClient());
        }

        public IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service)
        {
            return new AudioFileReader(song.FullPath);
        }

        public IEnumerable<string> LoadFilePaths(string path, IAudioService service)
        {
            if (File.Exists(path)) yield return path;

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (IsNotHidden(file)) yield return file;
                }
            }
        }

        private static bool IsNotHidden(string path)
        {
            FileInfo file = new FileInfo(path);

            return (file.Attributes & FileAttributes.Hidden) == 0;
        }

        public IMqttServer CreateMqttServer(IMqttAudioService service)
        {
            return new MqttServer(new MqttFactory().CreateMqttServer());
        }
    }
}
