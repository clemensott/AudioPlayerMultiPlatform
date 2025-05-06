using System;
using System.Threading.Tasks;
using AudioPlayerBackend;
using Avalonia.Threading;

namespace AudioPlayerFrontendAvalonia.Join;

public class InvokeDispatcherService: IInvokeDispatcherService
{
    public async Task InvokeDispatcher(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    public async Task<T> InvokeDispatcher<T>(Func<T> func)
    {
        return await Dispatcher.UIThread.InvokeAsync(func);
    }
}