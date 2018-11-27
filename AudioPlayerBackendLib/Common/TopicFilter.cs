namespace AudioPlayerBackend.Common
{
    class TopicFilter
    {
        public string Topic { get; private set; }

        public MqttQualityOfServiceLevel Qos { get; private set; }

        public TopicFilter(string topic, MqttQualityOfServiceLevel qos)
        {
            Topic = topic;
            Qos = qos;
        }
    }
}