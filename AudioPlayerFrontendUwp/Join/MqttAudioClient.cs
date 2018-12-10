using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class MqttAudioClient : AudioPlayerBackend.MqttAudioClient
    {
        public MqttAudioClient(IPlayer player, string serverAddress, int? port = null) : base(player, serverAddress, port)
        {
        }

        protected override IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format)
        {
            return new BufferedWaveProvider(format);
        }

        protected override IMqttClient CreateMqttClient()
        {
            return new MqttClient(new MQTTnet.MqttFactory().CreateMqttClient());
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
