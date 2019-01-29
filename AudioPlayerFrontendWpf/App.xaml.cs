using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace AudioPlayerFrontend
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            base.OnStartup(e);
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("DispatcherException.log", e.Exception.ToString());
        }
    }
}
