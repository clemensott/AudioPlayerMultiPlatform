using System;
using System.Collections.Generic;
using System.IO;
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
    public class OwnTcpClientCommunicator : OwnTcpCommunicator, IClientCommunicator
    {
        private bool isSyncing, isSynced;
        private TcpClient client;
        private OwnTcpSendQueue sendQueue;
        private AsyncQueue<OwnTcpMessage> processQueue;
        private Task openTask;

        public override event EventHandler<DisconnectedEventArgs> Disconnected;

        public override bool IsOpen => client?.Connected ?? false;

        public override string Name => $"TCP: {ServerAddress.Trim()}:{Port}";

        public string ServerAddress { get; }

        public int? Port { get; }

        public OwnTcpClientCommunicator(string serverAddress, int port, INotifyPropertyChangedHelper helper = null)
            : base(helper)
        {
            ServerAddress = serverAddress;
            Port = port;
        }

        public override async Task OpenAsync(BuildStatusToken statusToken)
        {
            if (IsOpen) return;

            try
            {
                isSynced = false;

                IPAddress address;
                if (!IPAddress.TryParse(ServerAddress, out address)) address = await GetIpAddress(ServerAddress);

                sendQueue = new OwnTcpSendQueue();
                client = new TcpClient();
                await client.ConnectAsync(address, Port ?? -1);
            }
            catch
            {
                client?.Dispose();
                client = null;
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
                isSynced = false;

                NetworkStream stream = client.GetStream();
                Dictionary<uint, OwnTcpSendMessage> waitDict = new Dictionary<uint, OwnTcpSendMessage>();
                processQueue = new AsyncQueue<OwnTcpMessage>();

                Task publishTask = Task.Run(() => SendMessagesHandler(stream, sendQueue, waitDict));
                Task receiveTask = Task.Run(() => ReceiveHandler(stream, waitDict, processQueue));
                Task processTask = ProcessHandler(processQueue);

                await SendCommand(syncCmd, false);
                isSynced = true;

                openTask = Task.WhenAll(publishTask, receiveTask, processTask);
            }
            catch (Exception e)
            {
                try
                {
                    await CloseAsync(e, true);
                }
                catch { }

                throw;
            }
            finally
            {
                isSyncing = false;
            }
        }

        public override Task CloseAsync()
        {
            return CloseAsync(null, true);
        }

        private async Task CloseAsync(Exception e, bool awaitAll)
        {
            if (!IsOpen) return;

            isSynced = false;
            if (e == null) await SendCommand(closeCmd, true).ConfigureAwait(false);

            sendQueue.End();
            await processQueue.End().ConfigureAwait(false);

            client?.Dispose();
            client = null;

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
            DisposeTask().Wait();

            async Task DisposeTask()
            {
                await CloseAsync(null, false).ConfigureAwait(false);
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
            await sendQueue.Enqueue(message);
        }

        private async Task SendCommand(string cmd, bool fireAndForget)
        {
            if (!IsOpen) return;

            await sendQueue.Enqueue(OwnTcpMessage.FromCommand(cmd, fireAndForget)).ConfigureAwait(false);
        }

        protected override async Task SendAsync(string topic, byte[] payload, bool fireAndForget)
        {
            if (isSyncing || !IsOpen || IsTopicLocked(topic, payload)) return;

            await sendQueue.Enqueue(OwnTcpMessage.FromData(topic, payload, fireAndForget));
        }

        private async Task SendMessagesHandler(Stream stream,
            OwnTcpSendQueue queue, IDictionary<uint, OwnTcpSendMessage> waits)
        {
            try
            {
                uint count = 0;
                while (!queue.IsEnded)
                {
                    OwnTcpSendMessage send = queue.Dequeue();
                    if (queue.IsEnded) break;

                    send.Message.ID = count++;

                    if (!send.Message.IsFireAndForget) waits.Add(send.Message.ID, send);

                    byte[] data = GetBytes(send.Message).ToArray();
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();

                    if (send.Message.IsFireAndForget) send.SetValue(true);
                    if (send.Message.Topic == closeCmd) break;
                }
            }
            catch (Exception e)
            {
                await CloseAsync(new Exception("SendMessageHandler error", e), false);
            }
        }

        private async Task ReceiveHandler(Stream stream,
            Dictionary<uint, OwnTcpSendMessage> waits, AsyncQueue<OwnTcpMessage> queue)
        {
            try
            {
                while (client.Connected)
                {
                    OwnTcpMessage message = await ReadMessage(stream);
                    if (message == null || !client.Connected) break;

                    switch (message.Topic)
                    {
                        case anwserCmd:
                            int code = BitConverter.ToInt32(message.Payload, 0);

                            if (code == 200)
                            {
                                waits[message.ID].SetValue(true);
                                waits.Remove(message.ID);
                            }
                            else await CloseAsync(new Exception("Negative Answer"), false);
                            break;

                        case closeCmd:
                            Exception e = new Exception("Server sent close");
                            await CloseAsync(e, false);
                            return;

                        case syncCmd:
                            ByteQueue data = message.Payload;
                            data.DequeueService(Service, id => new Playlist(id, helper));
                            waits[message.ID].SetValue(true);
                            waits.Remove(message.ID);
                            break;

                        default:
                            if (isSynced) await queue.Enqueue(message);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await CloseAsync(new Exception("ReceiveHandler error", e), false);
            }
        }

        private async Task ProcessHandler(AsyncQueue<OwnTcpMessage> queue)
        {
            while (true)
            {
                (_, OwnTcpMessage item) = await queue.Dequeue();
                if (queue.IsEnd) break;

                try
                {
                    LockTopic(item.Topic, item.Payload);

                    bool success = HandlerMessage(item);

                    if (success) continue;

                    Exception e = new Exception($"Handle Message not successful. Topic: {item.Topic}");
                    await CloseAsync(e, false);
                }
                catch (Exception e)
                {
                    e = new Exception($"Handle Message error. Topic: {item.Topic}", e);
                    await CloseAsync(e, false);
                    break;
                }
                finally
                {
                    UnlockTopic(item.Topic);
                }
            }
        }
    }
}
