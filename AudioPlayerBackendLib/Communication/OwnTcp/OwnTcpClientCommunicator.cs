using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public class OwnTcpClientCommunicator : OwnTcpCommunicator, IClientCommunicator
    {
        private static readonly TimeSpan pingInterval = TimeSpan.FromSeconds(10);

        private OwnTcpClientConnection connection;
        private readonly IInvokeDispatcherService dispatcher;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public override bool IsOpen => connection?.Client.Connected ?? false;

        public override string Name => $"TCP: {ServerAddress.Trim()} : {Port}";

        public string ServerAddress { get; }

        public int? Port { get; }

        public OwnTcpClientCommunicator(string serverAddress, int port)
        {
            ServerAddress = serverAddress;
            Port = port;
            dispatcher = AudioPlayerServiceProvider.Current.GetDispatcher();
        }

        private static async Task<IPAddress> GetIpAddress(string serverAddress)
        {
            IPHostEntry entry = await Dns.GetHostEntryAsync(serverAddress);
            return entry.AddressList.First(a => a.AddressFamily.HasFlag(AddressFamily.InterNetwork) &&
                                                !a.AddressFamily.HasFlag(AddressFamily.InterNetworkV6));
        }

        public override async Task Start()
        {
            if (IsOpen) return;

            try
            {
                IPAddress address;
                if (!IPAddress.TryParse(ServerAddress, out address)) address = await GetIpAddress(ServerAddress);

                TcpClient client = new TcpClient();
                await client.ConnectAsync(address, Port ?? -1);

                connection = new OwnTcpClientConnection(client);
                connection.Disconnected += Connection_Disconnected;

                Task publishTask = Task.Run(() => SendMessagesHandler(connection));
                Task pingTask = Task.Run(() => SendPingsHandler(connection));
                Task receiveTask = Task.Run(() => ReceiveHandler(connection));
                Task processTask = Task.Run(() => ProcessHandler(connection));

                connection.Task = Task.WhenAll(publishTask, pingTask, receiveTask, processTask);
            }
            catch (Exception e)
            {
                await Stop();
                throw;
            }
        }

        public override async Task<bool> SendCommand(string cmd)
        {
            if (!IsOpen) return false;

            byte[] payload = Encoding.UTF8.GetBytes(cmd);
            await SendAsync(cmdString, payload, true);
            return true;
        }

        public override Task<byte[]> SendAsync(string topic, byte[] payload)
        {
            return SendAsync(topic, payload, false);
        }

        protected override async Task<byte[]> SendAsync(string topic, byte[] payload, bool fireAndForget)
        {
            if (!IsOpen || IsTopicLocked(topic, payload)) return null;

            uint id = connection.GetNextMessageID();
            return await connection.SendQueue.Enqueue(OwnTcpMessage.FromData(topic, payload, fireAndForget, id));
        }

        private static async Task SendMessagesHandler(OwnTcpClientConnection connection)
        {
            try
            {
                while (!connection.SendQueue.IsEnded)
                {
                    OwnTcpSendMessage send = connection.SendQueue.Dequeue();
                    if (connection.SendQueue.IsEnded) break;
                    if (!send.Message.IsFireAndForget) connection.Waits.Add(send.Message.ID, send);

                    byte[] data = GetBytes(send.Message).ToArray();
                    await connection.Stream.WriteAsync(data, 0, data.Length);
                    await connection.Stream.FlushAsync();

                    if (send.Message.IsFireAndForget) send.SetResult(null);
                    if (send.Message.Topic == CloseCmd) break;
                }
            }
            catch (Exception e)
            {
                await connection.CloseAsync(new Exception("SendMessageHandler error", e), false);
            }
        }

        private static async Task SendPingsHandler(OwnTcpClientConnection connection)
        {
            Task cancelTask = connection.PingSem.WaitAsync();
            try
            {
                while (connection.Client?.Connected == true)
                {
                    Task delayTask = Task.Delay(pingInterval);
                    await Task.WhenAny(delayTask, cancelTask);

                    if (cancelTask.IsCompleted) return;

                    await connection.SendCommand(PingCmd, false, TimeSpan.FromSeconds(2), cancelTask);
                }
            }
            catch (Exception e)
            {
                await connection.CloseAsync(new Exception("ReceiveHandler error", e), false);
            }
        }

        private async Task ReceiveHandler(OwnTcpClientConnection connection)
        {
            try
            {
                while (connection.Client?.Connected == true)
                {
                    OwnTcpMessage message = await connection.ReadMessage();
                    if (message == null || connection.Client?.Connected != true) break;

                    switch (message.Topic)
                    {
                        case AnwserCmd:
                            int code = BitConverter.ToInt32(message.Payload, 0);
                            byte[] payload = message.Payload.Skip(sizeof(int)).ToArray();

                            if (code == (int)HttpStatusCode.OK)
                            {
                                connection.Waits[message.ID].SetResult(payload);
                                connection.Waits.Remove(message.ID);
                            }
                            else if(code == (int)HttpStatusCode.InternalServerError)
                            {
                                string exceptionMessage = Encoding.UTF8.GetString(payload);
                                connection.Waits[message.ID].SetException(new Exception(exceptionMessage));
                                connection.Waits.Remove(message.ID);
                            }
                            else await connection.CloseAsync(new Exception("Negative Answer"), false);
                            break;

                        case CloseCmd:
                            Exception e = new Exception("Server sent close");
                            await connection.CloseAsync(e, false);
                            return;

                        default:
                            await connection.ProcessQueue.Enqueue(message);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await connection.CloseAsync(new Exception("ReceiveHandler error", e), false);
            }
        }

        private async Task ProcessHandler(OwnTcpClientConnection connection)
        {
            while (true)
            {
                (_, OwnTcpMessage item) = await connection.ProcessQueue.Dequeue().ConfigureAwait(false);
                if (connection.ProcessQueue.IsEnd) break;

                try
                {
                    LockTopic(item.Topic, item.Payload);

                    bool handleAction() => HandlerMessage(item);
                    bool success = await dispatcher.InvokeDispatcher(handleAction);

                    if (success) continue;

                    Exception e = new Exception($"Handle Message not successful. Topic: {item.Topic}");
                    await connection.CloseAsync(e, false);
                }
                catch (Exception e)
                {
                    e = new Exception($"Handle Message error. Topic: {item.Topic}", e);
                    await connection.CloseAsync(e, false);
                    break;
                }
                finally
                {
                    UnlockTopic(item.Topic);
                }
            }
        }

        private void Connection_Disconnected(object sender, DisconnectedEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        public override async Task Stop()
        {
            if (connection == null) return;

            connection.Disconnected -= Connection_Disconnected;
            await connection.CloseAsync(null, true).ConfigureAwait(false);
            connection = null;
        }

        public override async Task Dispose()
        {
            await Stop();
        }
    }
}
