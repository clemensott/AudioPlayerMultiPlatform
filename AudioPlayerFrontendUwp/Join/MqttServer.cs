using AudioPlayerBackend.Common;
using System;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class MqttServer : IMqttServer
    {
        private MQTTnet.Server.IMqttServer parent;

        public MqttServer(MQTTnet.Server.IMqttServer parent)
        {
            this.parent = parent;
        }

        public async Task PublishAsync(MqttApplicationMessage message)
        {
            await parent.PublishAsync(message.ToFrontend());
        }

        public async Task StartAsync(int port, Action<MqttApplicationMessageInterceptorContext> interception)
        {
            MQTTnet.Server.IMqttServerOptions options = new MQTTnet.Server.MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(port)
                .WithApplicationMessageInterceptor((frontContext) =>
                {
                    var backContext = frontContext.ToBackend();
                    interception(backContext);
                    backContext.ToFrontend(frontContext);
                })
                .Build();

            await parent.StartAsync(options);
        }

        public async Task StopAsync()
        {
            await parent.StopAsync();
        }
    }
}
