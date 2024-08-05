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
    public class AudioServices : IAudioService
    {
        public IServiceProvider ServiceProvider { get; }

        public IEnumerable<IAudioService> Services { get; }

        public AudioServices(IServiceProvider serviceProvider, IEnumerable<IAudioService> services)
        {
            ServiceProvider = serviceProvider;
            Services = services;
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

        public IServicedLibraryRepo GetServicedLibraryRepo()
        {
            return ServiceProvider.GetService<IServicedLibraryRepo>();
        }

        public IServicedPlaylistsRepo GetServicedPlaylistsRepo()
        {
            return ServiceProvider.GetService<IServicedPlaylistsRepo>();
        }

        public Task Start()
        {
            return Task.WhenAll(Services.Select(s => s.Start()));
        }

        public Task Stop()
        {
            return Task.WhenAll(Services.Select(s => s.Stop()));
        }

        public Task Dispose()
        {
            return Task.WhenAll(Services.Select(s => s.Dispose()));
        }
    }
}
