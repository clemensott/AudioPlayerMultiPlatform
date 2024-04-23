﻿using System.Windows.Threading;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilderHelper : IServiceBuilderHelper
    {
        public ServiceBuilderHelper(Dispatcher dispatcher = null)
        {
            if (dispatcher != null) Dispatcher = new InvokeDispatcherHelper(dispatcher);
        }

        public IInvokeDispatcherService Dispatcher { get; }

        public AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service)
        {
            return new AudioServicePlayer(service, player);
        }

        public AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service)
        {
            return new AudioStreamPlayer(service, player);
        }
    }
}
