using System;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("AppDomainUnhandledException.log", e.ExceptionObject?.ToString() ?? "null");
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("DispatcherUnhandledException.log", e.Exception.ToString());
        }
    }
}
