using AudioPlayerBackend.Audio;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AudioPlayerBackend
{
    public class AudioPlayerServiceProvider
    {

        private static AudioPlayerServiceProvider instance;

        public static AudioPlayerServiceProvider Current
        {
            get
            {
                if (instance == null) instance = new AudioPlayerServiceProvider();

                return instance;
            }
        }

        private readonly ServiceCollection services;
        private IServiceProvider serviceProvider;

        private AudioPlayerServiceProvider()
        {
            services = new ServiceCollection();

            AddInternalServices();
        }

        #region Internal Service
        private void AddInternalServices()
        {
            AddAudioCreateService<AudioCreateService>();
        }

        private static Exception NotBuildYetException()
        {
            return new InvalidOperationException("Service provider is not build yet.");
        }

        private AudioPlayerServiceProvider AddAudioCreateService<TImplementation>() where TImplementation : class, IAudioCreateService
        {
            services.AddTransient<IAudioCreateService, TImplementation>();
            return this;
        }

        public IAudioCreateService GetAudioCreateService()
        {
            return serviceProvider?.GetService<IAudioCreateService>() ?? throw NotBuildYetException();
        }
        #endregion

        #region External Service
        public AudioPlayerServiceProvider AddFileSystemService<TImplementation>() where TImplementation : class, IFileSystemService
        {
            services.AddSingleton<IFileSystemService, TImplementation>();
            return this;
        }

        public IFileSystemService GetFileSystemService()
        {
            return serviceProvider?.GetService<IFileSystemService>() ?? throw NotBuildYetException();
        }

        public AudioPlayerServiceProvider AddPlayerCreateService<TImplementation>() where TImplementation : class, IPlayerCreateService
        {
            services.AddTransient<IPlayerCreateService, TImplementation>();
            return this;
        }

        public IPlayerCreateService GetPlayerCreateService()
        {
            return serviceProvider?.GetService<IPlayerCreateService>() ?? throw NotBuildYetException();
        }

        public AudioPlayerServiceProvider AddDispatcher<TImplementation>() where TImplementation : class, IInvokeDispatcherService
        {
            services.AddTransient<IInvokeDispatcherService, TImplementation>();
            return this;
        }

        public IInvokeDispatcherService GetDispatcher()
        {
            return serviceProvider?.GetService<IInvokeDispatcherService>() ?? throw NotBuildYetException();
        }
        #endregion

        public void Build()
        {
            if (serviceProvider != null)
            {
                throw new InvalidOperationException("Service are already built");
            }

            serviceProvider = services.BuildServiceProvider();
        }
    }
}
