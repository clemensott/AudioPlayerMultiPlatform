using AudioPlayerBackend;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AudioPlayerFrontend.Join
{
    class NotifyPropertyChangedHelper : INotifyPropertyChangedHelper
    {
        public Action<Action> InvokeDispatcher => DoInvokeDispatcher;

        protected NotifyPropertyChangedHelper()
        {
        }

        private static async void DoInvokeDispatcher(Action action)
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
    }
}
