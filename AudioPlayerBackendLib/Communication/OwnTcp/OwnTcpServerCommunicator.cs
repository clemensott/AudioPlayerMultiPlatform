using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Build;
using StdOttStandard.Linq;
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
        public override event EventHandler<ReceivedEventArgs> Received;

        public override bool IsOpen => isOpen;

        public int Port { get; }

        public override string Name => "TCP Server: " + Port;

        public OwnTcpServerCommunicator(AudioServicesBuildConfig config)
        {
            Port = config.ServerPort;
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public override async Task Start()
        {
            if (isOpen) return;

            try
            {
                isOpen = true;
                listener.Start();

                connections = new List<OwnTcpServerConnection>();
                processQueue = new AsyncQueue<OwnTcpSendMessage>();

                openTask = Task.WhenAll(ProcessHandler(processQueue), NewConnectionsHandler(processQueue));
            }
            catch (Exception e)
            {
                await Stop(e, true);
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
                await Stop(e, false);
            }
        }

        private async Task SendClientHandler(OwnTcpServerConnection connection)
        {
            try
            {
                while (connection.Client.Connected)
                {
                    OwnTcpSendMessage send = connection.SendQueue.Dequeue();
                    if (send == null || !connection.Client.Connected) break;

                    if (!send.Message.HasID) send.Message.ID = connection.GetNextMessageID();

                    byte[] data = GetBytes(send.Message).ToArray();
                    await connection.Stream.WriteAsync(data, 0, data.Length);
                    await connection.Stream.FlushAsync();

                    send.SetResult(null);
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
                            await SendAnswer(connection, message.ID, (int)HttpStatusCode.OK);
                            break;

                        case CloseCmd:
                            await CloseConnection(connection, false);
                            return;

                        default:
                            OwnTcpSendMessage processItem = new OwnTcpSendMessage(message);
                            await processQueue.Enqueue(processItem);

                            byte[] resultData = await processItem.Task;
                            Task responseTask = message.IsFireAndForget
                                ? Task.CompletedTask
                                : SendAnswer(connection, message.ID, (int)HttpStatusCode.OK, resultData);

                            await SendMessageToAllOtherClients(connection, message.Topic, message.Payload);

                            await responseTask;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await CloseConnection(connection);
            }
        }

        private static Task SendAnswer(OwnTcpServerConnection connection, uint id, int code)
        {
            byte[] payload = BitConverter.GetBytes(code);

            return SendMessageToClient(connection, AnwserCmd, id, payload);
        }

        private static Task SendAnswer(OwnTcpServerConnection connection, uint id, int code, byte[] data)
        {
            byte[] payload = BitConverter.GetBytes(code).Concat(data.ToNotNull()).ToArray();

            return SendMessageToClient(connection, AnwserCmd, id, payload);
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

        private Task<bool> SendMessageToAllOtherClients(OwnTcpServerConnection srcConnection, string topic, byte[] payload)
        {
            return SendMessageToClients(connections.Where(c => c != srcConnection), topic, payload);
        }

        private Task<bool> SendMessageToClients(string topic, byte[] payload)
        {
            return SendMessageToClients(connections, topic, payload);
        }

        private async Task<bool> SendMessageToClients(IEnumerable<OwnTcpServerConnection> sendToConnections, string topic, byte[] payload)
        {
            bool[] result = await Task.WhenAll(sendToConnections.ToArray().Select(Send));
            return result.All(x => x);

            async Task<bool> Send(OwnTcpServerConnection connection)
            {
                try
                {
                    return await SendMessageToClient(connection, topic, payload);
                }
                catch
                {
                    await CloseConnection(connection);
                    return false;
                }
            }
        }

        private static Task<bool> SendMessageToClient(OwnTcpServerConnection connection, string topic, byte[] payload)
        {
            return SendMessageToClient(connection, topic, 0, payload);
        }

        private static async Task<bool> SendMessageToClient(OwnTcpServerConnection connection,
            string topic, uint id, byte[] payload = null)
        {
            if (!connection.Client.Connected) return false;

            OwnTcpMessage message = new OwnTcpMessage()
            {
                IsFireAndForget = true,
                ID = id,
                Topic = topic,
                Payload = payload,
            };

            await connection.SendQueue.Enqueue(message).ConfigureAwait(false);
            return true;
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

                    ReceivedEventArgs args = new ReceivedEventArgs(Name, item.Message.Payload);
                    Received?.Invoke(this, args);
                    if (args.IsAwserStarted)
                    {
                        byte[] result = await args.Anwser.Task;
                        item.SetResult(result);
                    }
                    else item.SetException(new Exception("Handle message not successful"));

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

        public override Task Stop()
        {
            return Stop(null, true);
        }

        private async Task Stop(Exception e, bool awaitAll)
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

        public override async Task Dispose()
        {
            await Stop(null, false);
        }

        public override Task<bool> SendCommand(string cmd)
        {
            byte[] payload = Encoding.UTF8.GetBytes(cmd);
            return SendMessageToClients(cmdString, payload);
        }

        public override Task<byte[]> SendAsync(string topic, byte[] payload)
        {
            return SendAsync(topic, payload, false);
        }

        protected override async Task<byte[]> SendAsync(string topic, byte[] payload, bool fireAndForget)
        {
            await SendMessageToClients(topic, payload);
            return null;
        }
    }
}
