using System.Runtime.Serialization;

namespace AudioPlayerBackendLib
{
    [DataContract]
    public enum PlayState
    {
        Play,
        Pause,
        Stop
    }
}
