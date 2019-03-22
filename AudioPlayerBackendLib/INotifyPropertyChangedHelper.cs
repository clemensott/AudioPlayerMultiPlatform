using System;

namespace AudioPlayerBackend
{
    public interface INotifyPropertyChangedHelper
    {
        Action<Action> InvokeDispatcher { get; }
    }
}
