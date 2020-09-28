using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

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

        private static IRandomAccessStream OpenFileStream(string path)
        {
            Task<IRandomAccessStream> task = Task.Run(() => OpenFileStreamAsync(path));

            task.Wait();
            return task.Result;
        }

        private static async Task<IRandomAccessStream> OpenFileStreamAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            return await file.OpenAsync(FileAccessMode.Read);
        }

        public void Reload(ISourcePlaylistBase playlist)
        {
        }

        public void Update(ISourcePlaylistBase playlist)
        {
        }
    }
}
