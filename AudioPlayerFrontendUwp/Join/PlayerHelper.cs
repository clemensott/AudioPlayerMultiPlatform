using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class PlayerHelper : NotifyPropertyChangedHelper, IAudioServicePlayerHelper, IAudioStreamHelper
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

        public Action<IServicePlayer> SetWannaSongThreadSafe => null;

        public IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format, IAudioServiceBase service)
        {
            return new BufferedWaveProvider(format);
        }

        public IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service)
        {
            return new AudioFileReader(OpenFileStream(song.FullPath));
        }

        public IEnumerable<string> LoadFilePaths(string path, IAudioService service)
        {
            Task<IEnumerable<string>> task = Task.Run(() =>
            {
                return LoadFilePathsAsync(path);
            });

            task.Wait();

            return task.Result;
        }

        private static async Task<IEnumerable<string>> LoadFilePathsAsync(string path)
        {
            List<string> paths = new List<string>();

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                paths.Add(file.Path);
            }
            catch { }

            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                foreach (StorageFile file in await folder.GetFilesAsync())
                {
                    paths.Add(file.Path);
                }
            }
            catch { }

            return paths;
        }

        //public void DoSetCurrentSongThreadSafe(IServicePlayer service)
        //{
        //    IPlayer player = service.ServicePlayer;
        //    AudioFileReader reader = (AudioFileReader)service.Reader;
        //    Song? currentSong = service?.CurrentPlaylist?.CurrentSong;
        //    try
        //    {
        //        if (reader == null)
        //        {
        //            if (!currentSong.HasValue) return;

        //            player.Play(() =>
        //            {
        //                service.Reader = reader = (AudioFileReader)CreateWaveProvider(currentSong.Value, service);
        //                service.CurrentPlaylist.Duration = reader.TotalTime;

        //                return reader;
        //            });
        //        }
        //        else
        //        {
        //            IDisposable oldStream = reader.Stream;

        //            if (currentSong.HasValue) reader.Stream = OpenFileStream(currentSong.Value.FullPath);

        //            oldStream?.Dispose();
        //            player.ExecutePlayState();

        //            service.CurrentPlaylist.Duration = reader.TotalTime;
        //        }
        //    }
        //    catch
        //    {
        //        if (reader?.Stream != null)
        //        {
        //            player.Stop(reader);
        //            reader.Stream = null;
        //        }
        //    }
        //}

        private static IRandomAccessStream OpenFileStream(string path)
        {
            Task<IRandomAccessStream> task = Task.Run(() => OpenFileStreamAsync(path));

            task.Wait();
            return task.Result;
        }

        private async static Task<IRandomAccessStream> OpenFileStreamAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            return await file.OpenAsync(FileAccessMode.Read);
        }
    }
}
