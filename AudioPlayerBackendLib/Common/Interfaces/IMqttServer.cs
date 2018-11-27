using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Common
{
    public interface IMqttServer
    {
        Task StopAsync();
        Task StartAsync(int port, Action<MqttApplicationMessageInterceptorContext> interception);
        Task PublishAsync(MqttApplicationMessage message);
    }
}
