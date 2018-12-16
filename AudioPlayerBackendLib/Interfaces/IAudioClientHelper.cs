using System;

namespace AudioPlayerBackend
{
    public interface IAudioClientHelper
    {
        Action<Action> InvokeDispatcher { get; }
    }
}
