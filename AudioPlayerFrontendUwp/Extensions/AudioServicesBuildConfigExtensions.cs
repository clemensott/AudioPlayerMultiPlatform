using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend.Extensions
{
    static class AudioServicesBuildConfigExtensions
    {
        public static AudioServicesBuildConfig WithServiceProfile(this AudioServicesBuildConfig config, ServiceProfile profile)
        {
            // TODO: check if new options are relevant here
            if (profile.BuildStandalone) config.WithStandalone();
            else if (profile.BuildServer) config.WithServer(profile.ServerPort);
            else if (profile.BuildClient) config.WithClient(profile.ServerAddress, profile.ClientPort);

            return config
                .WithShuffle((OrderType?)profile.Shuffle)
                .WithIsSearchShuffle(profile.IsSearchShuffle)
                .WithPlay(profile.Play)
                .WithServerPort(profile.ServerPort)
                .WithClientPort(profile.ClientPort)
                .WithSearchKey(profile.SearchKey)
                .WithServerAddress(profile.ServerAddress)
                .WithVolume(profile.Volume);
        }

        public static ServiceProfile ToServiceProfile(this AudioServicesBuildConfig config)
        {
            return new ServiceProfile()
            {
                BuildStandalone = config.BuildStandalone,
                BuildServer = config.BuildServer,
                BuildClient = config.BuildClient,
                Shuffle = (int?)config.Shuffle,
                IsSearchShuffle = config.IsSearchShuffle,
                Play = config.Play,
                ServerPort = config.ServerPort,
                ClientPort = config.ClientPort,
                SearchKey = config.SearchKey,
                ServerAddress = config.ServerAddress,
                Volume = config.Volume,
                ClientVolume = null,
            };
        }
    }
}
