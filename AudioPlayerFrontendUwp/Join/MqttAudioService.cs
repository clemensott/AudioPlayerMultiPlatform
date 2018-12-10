using System;
using System.Collections.Generic;
using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class MqttAudioService : AudioPlayerBackend.MqttAudioService
    {
        private new AudioFileReader reader;

        public MqttAudioService(IPlayer player, int port) : base(player, port)
        {
        }

        protected override IMqttServer CreateMqttServer()
        {
            return new MqttServer(new MQTTnet.MqttFactory().CreateMqttServer());
        }

        protected override IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return new AudioFileReader(AudioService.OpenFileStream(song.FullPath));
        }

        protected override IEnumerable<string> LoadFilePaths(string path)
        {
            return AudioService.LoadFilePathsStatic(path);
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

                    if (CurrentSong.HasValue) reader.Stream = AudioService.OpenFileStream(CurrentSong.Value.FullPath);

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
