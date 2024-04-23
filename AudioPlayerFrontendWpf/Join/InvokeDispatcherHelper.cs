using AudioPlayerBackend;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AudioPlayerFrontend.Join
{
    class InvokeDispatcherHelper : IInvokeDispatcherService
    {
        private readonly Dispatcher dispatcher;

        public InvokeDispatcherHelper(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public async Task InvokeDispatcher(Action action)
        {
            await dispatcher.InvokeAsync(action);
        }

        public async Task<T> InvokeDispatcher<T>(Func<T> func)
        {
            return await dispatcher.InvokeAsync(func);
        }
    }
}
