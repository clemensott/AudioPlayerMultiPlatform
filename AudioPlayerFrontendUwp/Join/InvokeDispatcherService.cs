using AudioPlayerBackend;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class InvokeDispatcherService : IInvokeDispatcherService
    {
        public async Task InvokeDispatcher(Action action)
        {
            try
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) action();
                else
                {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
                }
            }
            catch { }
        }

        public async Task<T> InvokeDispatcher<T>(Func<T> func)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            T result = default(T);
            if (dispatcher.HasThreadAccess) result = func();
            else
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    result = func();
                });
            }

            return result;
        }
    }
}
