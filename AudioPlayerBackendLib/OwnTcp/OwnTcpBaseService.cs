using AudioPlayerBackend.Communication;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.OwnTcp
{
    public abstract class OwnTcpBaseService : IAudioService
    {
        private readonly string topicPrefix;
        private readonly IClientCommunicator clientCommunicator;

        public OwnTcpBaseService(string topicPrefix, IClientCommunicator clientCommunicator)
        {
            this.topicPrefix = topicPrefix + ".";
            this.clientCommunicator = clientCommunicator;
        }

        public Task Start()
        {
            clientCommunicator.Received += ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            clientCommunicator.Received -= ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            return Stop();
        }

        private async void ClientCommunicator_Received(object sender, ReceivedEventArgs e)
        {
            TaskCompletionSource<byte[]> anwser = null;
            try
            {
                if (!e.Topic.StartsWith(topicPrefix)) return;
                string subTopic = e.Topic.Substring(topicPrefix.Length);

                anwser = e.StartAnwser();

                byte[] result = await OnMessageReceived(subTopic, e.Payload);
                anwser.SetResult(result);
            }
            catch (Exception exception)
            {
                if (anwser != null) anwser.SetException(exception);
                else throw;
            }
        }

        protected abstract Task<byte[]> OnMessageReceived(string subTopic, byte[] payload);

        protected Task<byte[]> SendAsync(string subTopic, byte[] payload = null)
        {
            return clientCommunicator.SendAsync($"{topicPrefix}{subTopic}", payload);
        }
    }
}
