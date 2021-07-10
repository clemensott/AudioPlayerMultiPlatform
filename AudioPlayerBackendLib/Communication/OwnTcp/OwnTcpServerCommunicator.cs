using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Communication.Base;
using StdOttStandard.Linq.DataStructures;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public class OwnTcpServerCommunicator : OwnTcpCommunicator, IServerCommunicator
    {
        private bool isOpen;
        private readonly TcpListener listener;
        private IList<OwnTcpServerConnection> connections;
        private AsyncQueue<OwnTcpSendMessage> processQueue;
        private Task openTask;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public override bool IsOpen => isOpen;

        public int Port { get; }

        public override string Name => "TCP Server: " + Port;

        public OwnTcpServerCommunicator(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
        {
            if (isOpen) return;

            try
            {
                isOpen = true;
                listener.Start();
            }
            catch
            {
                await CloseAsync();
                throw;
            }
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
                connections = new List<OwnTcpServerConnection>();
                processQueue = new AsyncQueue<OwnTcpSendMessage>();

                openTask = Task.WhenAll(ProcessHandler(processQueue), NewConnectionsHandler(processQueue));
            }
            catch (Exception e)
            {
                await CloseAsync(e, true);
                throw;
            }
        }

        private async Task NewConnectionsHandler(AsyncQueue<OwnTcpSendMessage> processQueue)
        {
            try
            {
                while (IsOpen)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    OwnTcpSendQueue queue = new OwnTcpSendQueue();
                    OwnTcpServerConnection connection = new OwnTcpServerConnection(client, queue);

                    connections.Add(connection);

                    Task sendTask = Task.Run(() => SendClientHandler(connection));
                    Task readTask = Task.Run(() => ReadClientHandler(connection, processQueue));
                    connection.Task = Task.WhenAll(sendTask, readTask);
                }
            }
            catch (Exception e)
            {
                await CloseAsync(e, false);
            }
        }

        private async Task SendClientHandler(OwnTcpServerConnection connection)
        {
            uint count = 0;
            try
            {
                while (connection.Client.Connected)
                {
                    OwnTcpSendMessage send = connection.SendQueue.Dequeue();
                    if (send == null || !connection.Client.Connected) break;

                    if (!send.Message.HasID) send.Message.ID = count++;

                    byte[] data = GetBytes(send.Message).ToArray();
                    await connection.Stream.WriteAsync(data, 0, data.Length);
                    await connection.Stream.FlushAsync();

                    send.SetResult(true);
                }
            }
            catch (Exception e)
            {
                await CloseConnection(connection);
            }
        }

        private async Task ReadClientHandler(OwnTcpServerConnection connection, AsyncQueue<OwnTcpSendMessage> processQueue)
        {
            try
            {
                while (connection.Client.Connected)
                {
                    OwnTcpMessage message = await connection.ReadMessage();
                    if (message == null || !connection.Client.Connected) break;

                    switch (message.Topic)
                    {
                        case PingCmd:
                            await SendAnswer(connection, message.ID, 200);
                            break;

                        case SyncCmd:
                            ByteQueue data = new ByteQueue();
                            data.Enqueue(Service);
                            await SendMessageToClient(connection, SyncCmd, message.ID, data);
                            break;

                        case CloseCmd:
                            await CloseConnection(connection, false);
                            return;

                        default:
                            OwnTcpSendMessage processItem = new OwnTcpSendMessage(message);
                            await processQueue.Enqueue(processItem);

                            if (await processItem.Task)
                            {
                                Task responseTask = message.IsFireAndForget
                                    ? Task.CompletedTask
                                    : SendAnswer(connection, message.ID, 200);

                                await SendMessageToAllOtherClients(connection, message.Topic, message.Payload);

                                await responseTask;
                            }
                            else await CloseConnection(connection);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await CloseConnection(connection);
            }
        }

        private static Task SendAnswer(OwnTcpServerConnection connection, uint id, int value)
        {
            return SendMessageToClient(connection, AnwserCmd, id, BitConverter.GetBytes(value));
        }

        private async Task CloseConnection(OwnTcpServerConnection connection, bool sendClose = true)
        {
            if (!connections.Contains(connection)) return;

            try
            {
                if (sendClose) await SendMessageToClient(connection, CloseCmd, null).ConfigureAwait(false);
            }
            catch { }

            connection.SendQueue.End();
            connection.Client.Dispose();
            connections.Remove(connection);
        }

        private Task SendMessageToAllOtherClients(OwnTcpServerConnection srcConnection, string topic, byte[] payload)
        {
            return SendMessageToClients(connections.Where(c => c != srcConnection), topic, payload);
        }

        private Task SendMessageToClients(string topic, byte[] payload)
        {
            return SendMessageToClients(connections, topic, payload);
        }

        private Task SendMessageToClients(IEnumerable<OwnTcpServerConnection> sendToConnections, string topic, byte[] payload)
        {
            return Task.WhenAll(sendToConnections.ToArray().Select(Send));

            async Task Send(OwnTcpServerConnection connection)
            {
                try
                {
                    await SendMessageToClient(connection, topic, payload);
                }
                catch
                {
                    await CloseConnection(connection);
                }
            }
        }

        private static Task SendMessageToClient(OwnTcpServerConnection connection, string topic, byte[] payload)
        {
            return SendMessageToClient(connection, topic, 0, payload);
        }

        private static async Task SendMessageToClient(OwnTcpServerConnection connection,
            string topic, uint id, byte[] payload = null)
        {
            if (!connection.Client.Connected) return;

            OwnTcpMessage message = new OwnTcpMessage()
            {
                IsFireAndForget = true,
                ID = id,
                Topic = topic,
                Payload = payload,
            };

            await connection.SendQueue.Enqueue(message).ConfigureAwait(false);
        }

        private async Task ProcessHandler(AsyncQueue<OwnTcpSendMessage> queue)
        {
            while (true)
            {
                (_, OwnTcpSendMessage item) = await queue.Dequeue();
                if (queue.IsEnd) break;

                try
                {
                    LockTopic(item.Message.Topic, item.Message.Payload);

                    bool success = HandlerMessage(item.Message);
                    item.SetResult(success);
                }
                catch (Exception e)
                {
                    item.SetException(e);
                }
                finally
                {
                    UnlockTopic(item.Message.Topic);
                }
            }
        }

        public override Task CloseAsync()
        {
            return CloseAsync(null, true);
        }

        private async Task CloseAsync(Exception e, bool awaitAll)
        {
            if (!IsOpen) return;

            foreach (OwnTcpServerConnection connection in connections.ToArray())
            {
                await CloseConnection(connection).ConfigureAwait(false);
            }

            isOpen = false;
            listener.Stop();

            await processQueue.End();

            Task raiseTask = RaiseDisconnected();
            if (awaitAll) await raiseTask.ConfigureAwait(false);

            async Task RaiseDisconnected()
            {
                if (openTask != null) await openTask.ConfigureAwait(false);

                if (e != null) { }
                Disconnected?.Invoke(this, new DisconnectedEventArgs(e == null, e));
            }
        }

        public override void Dispose()
        {
            CloseAsync(null, false).Wait();
        }

        public override Task SendCommand(string cmd)
        {
            byte[] payload = Encoding.UTF8.GetBytes(cmd);
            return SendMessageToClients(cmdString, payload);
        }

        protected override Task SendAsync(string topic, byte[] payload, bool fireAndForget)
        {
            return SendMessageToClients(topic, payload);
        }
    }
}
