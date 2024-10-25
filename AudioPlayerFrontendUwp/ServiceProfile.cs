namespace AudioPlayerFrontend
{
    public struct ServiceProfile
    {
        public bool BuildStandalone { get; set; }

        public bool BuildServer { get; set; }

        public bool BuildClient { get; set; }

        public int? Shuffle { get; set; }

        public bool? IsSearchShuffle { get; set; }

        public bool? Play { get; set; }

        public int ServerPort { get; set; }

        public int? ClientPort { get; set; }

        public string SearchKey { get; set; }

        public string ServerAddress { get; set; }

        public float? Volume { get; set; }

        public float? ClientVolume { get; set; }
    }
}
