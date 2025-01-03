﻿using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication.Base;
using System.Linq;

namespace AudioPlayerFrontend
{
    public struct ServiceProfile
    {
        public bool BuildStandalone { get; set; }

        public bool BuildServer { get; set; }

        public bool BuildClient { get; set; }

        public bool AutoUpdate { get; set; }

        public int ServerPort { get; set; }

        public int? ClientPort { get; set; }

        public string ServerAddress { get; set; }

        public FileMediaSourceRootInfo[] DefaultUpdateRoots { get; set; }

        public byte[] ToData()
        {
            return new ByteQueue()
                .Enqueue(BuildStandalone)
                .Enqueue(BuildServer)
                .Enqueue(BuildClient)
                .Enqueue(AutoUpdate)
                .Enqueue(ServerPort)
                .Enqueue(ClientPort)
                .Enqueue(ServerAddress)
                .Enqueue(DefaultUpdateRoots);
        }

        public static ServiceProfile FromData(byte[] data)
        {
            ByteQueue queue = data;
            return new ServiceProfile()
            {
                BuildStandalone = queue.DequeueBool(),
                BuildServer = queue.DequeueBool(),
                BuildClient = queue.DequeueBool(),
                AutoUpdate = queue.DequeueBool(),
                ServerPort = queue.DequeueInt(),
                ClientPort = queue.DequeueIntNullable(),
                ServerAddress = queue.DequeueString(),
                DefaultUpdateRoots = queue.DequeueFileMediaSourceRootInfos()?.ToArray(),
            };
        }
    }
}
