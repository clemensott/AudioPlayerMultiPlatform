using AudioPlayerBackend.Communication;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.OwnTcp
{
    internal abstract class OwnTcpBaseServerConnector : IAudioService
    {
        private readonly string topicPrefix;
        private readonly IServerCommunicator serverCommunicator;

        public OwnTcpBaseServerConnector(string topicPrefix, IServerCommunicator serverCommunicator)
        {
            this.topicPrefix = topicPrefix + ".";
            this.serverCommunicator = serverCommunicator;
        }

        public Task Start()
        {
            SubscribeToService();

            serverCommunicator.Received += ServerCommunicator_Received;

            return Task.CompletedTask;
        }

        protected abstract void SubscribeToService();

        public Task Stop()
        {
            serverCommunicator.Received += ServerCommunicator_Received;

            UnsubscribeFromService();

            return Task.CompletedTask;
        }

        protected abstract void UnsubscribeFromService();

        public Task Dispose()
        {
            return Stop();
        }

        private async void ServerCommunicator_Received(object sender, ReceivedEventArgs e)
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

        protected Task<byte[]> SendAsync(string funcName, byte[] payload = null)
        {
            return serverCommunicator.SendAsync($"{topicPrefix}{funcName}", payload);
        }
    }
}
