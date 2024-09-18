﻿namespace AudioPlayerBackend.Communication.OwnTcp
{
    public class OwnTcpMessage
    {
        private uint id;

        public bool IsFireAndForget { get; set; }

        public bool HasID { get; private set; }

        public uint ID
        {
            get => id;
            set
            {
                id = value;
                HasID = true;
            }
        }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public static OwnTcpMessage FromCommand(string command, bool fireAndForget)
        {
            return new OwnTcpMessage()
            {
                IsFireAndForget = fireAndForget,
                Topic = command,
                Payload = null,
            };
        }

        public static OwnTcpMessage FromData(string topic, byte[] payload, bool fireAndForget, uint id = 0)
        {
            return new OwnTcpMessage()
            {
                IsFireAndForget = fireAndForget,
                ID = id,
                Topic = topic,
                Payload = payload,
            };
        }
    }
}
