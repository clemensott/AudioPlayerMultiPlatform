using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Communication.Base;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public class OwnTcpClientCommunicator : OwnTcpCommunicator, IClientCommunicator
    {
        private static readonly TimeSpan pingInterval = TimeSpan.FromSeconds(10);

        private bool isSyncing;
        private OwnTcpClientConnection connection;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public override bool IsOpen => connection?.Client.Connected ?? false;

        public override string Name => $"TCP: {ServerAddress.Trim()} : {Port}";

        public string ServerAddress { get; }

        public int? Port { get; }

        public OwnTcpClientCommunicator(string serverAddress, int port)
        {
            ServerAddress = serverAddress;
            Port = port;
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
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
            }
            catch
            {
                if (connection != null)
                {
                    connection.Disconnected -= Connection_Disconnected;
                    connection.Client.Dispose();
                    connection = null;
                }
                throw;
            }
        }

        private static async Task<IPAddress> GetIpAddress(string serverAddress)
        {
            IPHostEntry entry = await Dns.GetHostEntryAsync(serverAddress);
            return entry.AddressList.First(a => a.AddressFamily.HasFlag(AddressFamily.InterNetwork) &&
                                                !a.AddressFamily.HasFlag(AddressFamily.InterNetworkV6));
        }

        public override Task SetService(IAudioServiceBase service, BuildStatusToken statusToken)
        {
            Unsubscribe(Service);
            Service = service;
            Subscribe(Service);

            return SyncService(statusToken);
        }

        public override async Task SyncService(BuildStatusToken statusToken)
        {
            try
            {
                isSyncing = true;
                connection.IsSynced = false;
                connection.Service = Service;

                Task publishTask = Task.Run(() => SendMessagesHandler(connection));
                Task pingTask = Task.Run(() => SendPingsHandler(connection));
                Task receiveTask = Task.Run(() => ReceiveHandler(connection));
                Task processTask = Task.Run(() => ProcessHandler(connection));

                await connection.SendCommand(SyncCmd, false, TimeSpan.FromSeconds(10), statusToken.EndTask);
                connection.IsSynced = true;

                connection.Task = Task.WhenAll(publishTask, pingTask, receiveTask, processTask);
            }
            catch (Exception e)
            {
                try
                {
                    await connection.CloseAsync(e, true);
                }
                catch { }

                throw;
            }
            finally
            {
                isSyncing = false;
            }
        }

        public override async Task SendCommand(string cmd)
        {
            if (!IsOpen) return;

            byte[] payload = Encoding.UTF8.GetBytes(cmd);
            OwnTcpMessage message = new OwnTcpMessage()
            {
                IsFireAndForget = true,
                Topic = cmdString,
                Payload = payload,
            };
            await connection.SendQueue.Enqueue(message);
        }

        protected override async Task SendAsync(string topic, byte[] payload, bool fireAndForget)
        {
            if (isSyncing || !IsOpen || IsTopicLocked(topic, payload)) return;

            await connection.SendQueue.Enqueue(OwnTcpMessage.FromData(topic, payload, fireAndForget));
        }

        private static async Task SendMessagesHandler(OwnTcpClientConnection connection)
        {
            try
            {
                uint count = 0;
                while (!connection.SendQueue.IsEnded)
                {
                    OwnTcpSendMessage send = connection.SendQueue.Dequeue();
                    if (connection.SendQueue.IsEnded) break;

                    send.Message.ID = count++;

                    if (!send.Message.IsFireAndForget) connection.Waits.Add(send.Message.ID, send);

                    byte[] data = GetBytes(send.Message).ToArray();
                    await connection.Stream.WriteAsync(data, 0, data.Length);
                    await connection.Stream.FlushAsync();

                    if (send.Message.IsFireAndForget) send.SetValue(true);
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

                            if (code == 200)
                            {
                                connection.Waits[message.ID].SetValue(true);
                                connection.Waits.Remove(message.ID);
                            }
                            else await connection.CloseAsync(new Exception("Negative Answer"), false);
                            break;

                        case CloseCmd:
                            Exception e = new Exception("Server sent close");
                            await connection.CloseAsync(e, false);
                            return;

                        case SyncCmd:
                            ByteQueue data = message.Payload;
                            data.DequeueService(connection.Service, Service.CreateSourcePlaylist, Service.CreatePlaylist);

                            connection.Waits[message.ID].SetValue(true);
                            connection.Waits.Remove(message.ID);
                            break;

                        default:
                            if (connection.IsSynced) await connection.ProcessQueue.Enqueue(message);
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

                    bool success = HandlerMessage(item);

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

        public override async Task CloseAsync()
        {
            if (connection == null) return;

            connection.Disconnected -= Connection_Disconnected;
            await connection.CloseAsync(null, true).ConfigureAwait(false);
            connection = null;
        }

        public override void Dispose()
        {
            if (connection == null) return;
            DisposeTask().Wait();

            async Task DisposeTask()
            {
                connection.Disconnected -= Connection_Disconnected;
                await connection.CloseAsync(null, false).ConfigureAwait(false);
                connection = null;
            }
        }
    }
}
