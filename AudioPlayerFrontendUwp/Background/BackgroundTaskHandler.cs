using AudioPlayerBackend.Build;
using StdOttStandard.Dispatch;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Background
{
    class BackgroundTaskHandler
    {
        private readonly AudioServicesHandler servicesHandler;
        private readonly Dispatcher dispatcher;
        private TaskCompletionSource<bool> run;

        public bool IsRunning { get; private set; }

        public BackgroundTaskHandler(Dispatcher dispatcher, AudioServicesHandler servicesHandler)
        {
            this.dispatcher = dispatcher;
            this.servicesHandler = servicesHandler;
        }

        public async Task Run()
        {
            IsRunning = true;

            dispatcher.Start();

            run = new TaskCompletionSource<bool>();
            await run.Task;
            run = null;

            IsRunning = false;

            await dispatcher.Stop();
        }

        public void Stop()
        {
            run?.TrySetResult(true);
        }
    }
}
