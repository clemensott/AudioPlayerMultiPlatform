using AudioPlayerBackend.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;

namespace AudioPlayerBackend.Build
{
    public class AudioServices
    {
        public IServiceProvider ServiceProvider { get; }

        public IEnumerable<IAudioService> BackgroundServices { get; }

        public IEnumerable<IAudioService> ForegroundServices { get; }

        public IEnumerable<IAudioService> IntensiveServices { get; }

        public AudioServices(IServiceProvider serviceProvider, IEnumerable<IAudioService> services,
            IEnumerable<IAudioService> uiServiceList, IEnumerable<IAudioService> intensiveServices)
        {
            ServiceProvider = serviceProvider;
            BackgroundServices = services;
            ForegroundServices = uiServiceList;
            IntensiveServices = intensiveServices;
        }

        public IEnumerable<ICommunicator> GetCommunicators()
        {
            IClientCommunicator client = ServiceProvider.GetService<IClientCommunicator>();
            if (client != null) yield return client;

            IServerCommunicator server = ServiceProvider.GetService<IServerCommunicator>();
            if (server != null) yield return server;
        }

        public ILibraryViewModel GetViewModel()
        {
            return ServiceProvider.GetService<ILibraryViewModel>();
        }

        public IFileSystemService GetFileSystemService()
        {
            return ServiceProvider.GetService<IFileSystemService>();
        }

        public IUpdateLibraryService GetUpdateLibraryService()
        {
            return ServiceProvider.GetService<IUpdateLibraryService>();
        }

        public ILibraryRepo GetLibraryRepo()
        {
            return ServiceProvider.GetService<ILibraryRepo>();
        }

        public IPlaylistsRepo GetPlaylistsRepo()
        {
            return ServiceProvider.GetService<IPlaylistsRepo>();
        }

        private IEnumerable<IAudioService> GetAllAudioServices()
        {
            foreach (IAudioService service in BackgroundServices)
            {
                yield return service;
            }

            foreach (IAudioService service in ForegroundServices)
            {
                yield return service;
            }

            foreach (IAudioService service in IntensiveServices)
            {
                yield return service;
            }
        }

        private async Task Start(IEnumerable<IAudioService> services)
        {
            foreach (IAudioService service in services)
            {
                await service.Start();
            }
        }

        private async Task Stop(IEnumerable<IAudioService> services)
        {
            foreach (IAudioService service in services.Reverse())
            {
                await service.Stop();
            }
        }

        public async Task Start()
        {
            await Start(GetAllAudioServices());
        }

        public async Task Stop()
        {
            await Stop(GetAllAudioServices());
        }

        public async Task StartUiServices()
        {
            Logs.Log("StartUiServices2");
            await Start(ForegroundServices);
            Logs.Log("StartUiServices3");
        }

        public async Task StopUiServices()
        {
            Logs.Log("StopUiServices2");
            await Stop(ForegroundServices);
            Logs.Log("StopUiServices3");
        }

        public async Task StartIntensiveServices()
        {
            Logs.Log("StartIntensiveServices2");
            await Start(IntensiveServices);
            Logs.Log("StartIntensiveServices3");
        }

        public async Task StopIntensiveServices()
        {
            Logs.Log("StopIntensiveServices2");
            await Stop(IntensiveServices);
            Logs.Log("StopIntensiveServices3");
        }

        public Task Dispose()
        {
            Logs.Log("AudioServices.Dispose");
            return Task.WhenAll(GetAllAudioServices().Select(s => s.Dispose()));
        }
    }
}
