using AudioPlayerBackend.Communication.Base;

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

        public byte[] ToData()
        {
            return new ByteQueue()
                .Enqueue(BuildStandalone)
                .Enqueue(BuildServer)
                .Enqueue(BuildClient)
                .Enqueue(Shuffle)
                .Enqueue(IsSearchShuffle)
                .Enqueue(Play)
                .Enqueue(ServerPort)
                .Enqueue(ClientPort)
                .Enqueue(SearchKey)
                .Enqueue(ServerAddress)
                .Enqueue(Volume)
                .Enqueue(ClientVolume);
        }

        public static ServiceProfile FromData(byte[] data)
        {
            ByteQueue queue = data;
            return new ServiceProfile()
            {
                BuildStandalone = queue.DequeueBool(),
                BuildServer = queue.DequeueBool(),
                BuildClient = queue.DequeueBool(),
                Shuffle = queue.DequeueIntNullable(),
                IsSearchShuffle = queue.DequeueBoolNullable(),
                Play = queue.DequeueBoolNullable(),
                ServerPort = queue.DequeueInt(),
                ClientPort = queue.DequeueIntNullable(),
                SearchKey = queue.DequeueString(),
                ServerAddress = queue.DequeueString(),
                Volume = queue.DequeueFloatNullable(),
                ClientVolume = queue.DequeueFloatNullable(),
            };
        }
    }
}
