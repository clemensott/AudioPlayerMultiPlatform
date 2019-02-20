using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class AudioHelper : IMqttAudioClientHelper, IMqttAudioServiceHelper
    {
        public Action<IAudioService> SetCurrentSongThreadSafe => DoSetCurrentSongThreadSafe;

        public Action<Action> InvokeDispatcher => DoInvokeDispatcher;

        private async void DoInvokeDispatcher(Action action)
        {
            try
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) action();
                else
                {
            System.Diagnostics.Debug.WriteLine("DoInvokeDispatcher1");
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
            System.Diagnostics.Debug.WriteLine("DoInvokeDispatcher2");
                }
            }
            catch { }
        }

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

        public void DoSetCurrentSongThreadSafe(IAudioService service)
        {
            IPlayer player = service.Player;
            AudioFileReader reader = (AudioFileReader)service.Reader;
            Song? currentSong = service?.CurrentPlaylist?.CurrentSong;
            try
            {
                if (reader == null)
                {
                    if (!currentSong.HasValue) return;

                    player.Play(() =>
                    {
                        service.Reader = reader = (AudioFileReader)CreateWaveProvider(currentSong.Value, service);
                        service.CurrentPlaylist.Duration = reader.TotalTime;

                        return reader;
                    });
                }
                else
                {
                    IDisposable oldStream = reader.Stream;

                    if (currentSong.HasValue) reader.Stream = OpenFileStream(currentSong.Value.FullPath);

                    oldStream?.Dispose();
                    player.ExecutePlayState();

                    service.CurrentPlaylist.Duration = reader.TotalTime;
                }
            }
            catch
            {
                if (reader?.Stream != null)
                {
                    player.Stop(reader);
                    reader.Stream = null;
                }
            }
        }

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
        public IMqttServer CreateMqttServer(IMqttAudioService service)
        {
            return new MqttServer(new MqttFactory().CreateMqttServer());
        }
    }
}
