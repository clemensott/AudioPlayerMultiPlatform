using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class AudioService : AudioPlayerBackend.AudioService
    {
        private new AudioFileReader reader;

        public AudioService(IPlayer player) : base(player)
        {
        }

        protected override IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return new AudioFileReader(OpenFileStream(song.FullPath));
        }

        public static IRandomAccessStream OpenFileStream(string path)
        {
            Task<IRandomAccessStream> task = Task.Run(() => OpenFileStreamAsync(path));

            task.Wait();
            return task.Result;
        }

        public async static Task<IRandomAccessStream> OpenFileStreamAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            return await file.OpenAsync(FileAccessMode.Read);
        }

        protected override IEnumerable<string> LoadFilePaths(string path)
        {
            return LoadFilePathsStatic(path);
        }

        public static IEnumerable<string> LoadFilePathsStatic(string path)
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

        protected override void SetCurrentSongThreadSafe()
        {
            try
            {
                if (reader == null)
                {
                    if (!CurrentSong.HasValue) return;

                    Player.Play(() =>
                    {
                        base.reader = reader = (AudioFileReader)CreateWaveProvider(CurrentSong.Value);
                        Duration = reader.TotalTime;

                        return reader;
                    });
                }
                else
                {
                    IDisposable oldStream = reader.Stream;

                    if (CurrentSong.HasValue) reader.Stream = OpenFileStream(CurrentSong.Value.FullPath);

                    oldStream?.Dispose();
                    Player.ExecutePlayState();

                    Duration = reader.TotalTime;
                }
            }
            catch
            {
                if (reader?.Stream != null)
                {
                    Player.Stop(reader);
                    reader.Stream = null;
                }
            }
        }

        protected async override void InvokeDispatcher(Action action)
        {
            try
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) action();
                else await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
            }
            catch { }
        }
    }
}
