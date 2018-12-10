using System.Collections.Generic;
using System.IO;
using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerFrontend.Join
{
    class AudioService : AudioPlayerBackend.AudioService
    {
        public AudioService(IPlayer player) : base(player)
        {
        }

        protected override IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return new AudioFileReader(song.FullPath);
        }

        protected override IEnumerable<string> LoadFilePaths(string path)
        {
            return LoadFilePathsStatic(path);
        }

        public static IEnumerable<string> LoadFilePathsStatic(string path)
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
    }
}
