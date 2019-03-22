using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioPlayerFrontend.Join
{
    class AudioServiceHelper : NotifyPropertyChangedHelper, IAudioServiceHelper
    {
        private static AudioServiceHelper instance;

        public static AudioServiceHelper Current
        {
            get
            {
                if (instance == null) instance = new AudioServiceHelper();

                return instance;
            }
        }

        private AudioServiceHelper() { }

        private IEnumerable<string> LoadFilePaths(string path, IAudioService service)
        {
            Task<IEnumerable<string>> task = Task.Run(() =>
            {
                return LoadFilePathsAsync(path);
            });

            task.Wait();

            return task.Result;
        }

        private async static Task<IEnumerable<string>> LoadFilePathsAsync(string path)
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

        public void Reload(ISourcePlaylist playlist)
        {
            throw new NotImplementedException();
        }
    }
}
