using AudioPlayerBackend;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace AudioPlayerFrontend.Join
{
    internal class InvokeDispatcherService : IInvokeDispatcherService
    {
        public InvokeDispatcherService()
        {
        }

        public async Task InvokeDispatcher(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }

        public async Task<T> InvokeDispatcher<T>(Func<T> func)
        {
            return await Application.Current.Dispatcher.InvokeAsync(func);
        }
    }
}
