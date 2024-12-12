using AudioPlayerBackend.Build;
using AudioPlayerBackendUwpLib;

namespace AudioPlayerFrontend.Extensions
{
    static class AudioServicesBuildConfigExtensions
    {
        public static AudioServicesBuildConfig WithServiceProfile(this AudioServicesBuildConfig config, ServiceProfile profile)
        {
            if (profile.BuildStandalone) config.WithStandalone();
            else if (profile.BuildServer) config.WithServer(profile.ServerPort);
            else if (profile.BuildClient) config.WithClient(profile.ServerAddress, profile.ClientPort);

            return config
                .WithAutoUpdate(profile.AutoUpdate)
                .WithDefaultUpdateRoots(profile.DefaultUpdateRoots);
        }

        public static ServiceProfile ToServiceProfile(this AudioServicesBuildConfig config)
        {
            return new ServiceProfile()
            {
                BuildStandalone = config.BuildStandalone,
                BuildServer = config.BuildServer,
                BuildClient = config.BuildClient,
                AutoUpdate = config.AutoUpdate,
                ServerPort = config.ServerPort,
                ClientPort = config.ClientPort,
                ServerAddress = config.ServerAddress,
                DefaultUpdateRoots = config.DefaultUpdateRoots,
            };
        }
    }
}
