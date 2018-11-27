using System;
using System.Collections.Generic;
using System.Text;

namespace AudioPlayerBackend.Common
{
    public class MqttApplicationMessage
    {
        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Retain { get; set; }
    }
}
