using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerBackend
{
    public interface IMqttAudioServiceHelper : IAudioServiceHelper
    {
        IMqttServer CreateMqttServer(IMqttAudioService service);
    }
}
