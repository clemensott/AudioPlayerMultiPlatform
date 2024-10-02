using StdOttStandard.Linq.DataStructures;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpClientConnection : OwnTcpConnection
    {
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public IDictionary<uint, OwnTcpSendMessage> Waits { get; }

        public AsyncQueue<OwnTcpMessage> ProcessQueue { get; }

        public SemaphoreSlim PingSem { get; }

        public OwnTcpClientConnection(TcpClient client) : base(client)
        {
            Waits = new Dictionary<uint, OwnTcpSendMessage>();
            ProcessQueue = new AsyncQueue<OwnTcpMessage>();
            PingSem = new SemaphoreSlim(0);
        }

        /// <summary>
        /// Send a command and throws TimeoutException if time runs out or cancelTask finishes
        /// </summary>
        /// <param name="connection">Connection to send cmd with</param>
        /// <param name="cmd">Text which is send to server</param>
        /// <param name="fireAndForget">Don't wait for an answer</param>
        /// <param name="timeout">Duration after which a TimeoutException is thrown if sending has not finished</param>
        /// <param name="cancelTask">Task which causes a TimeoutException if it finishes before sending has finished</param>
        /// <returns></returns>
        public Task SendCommand(string cmd, bool fireAndForget, TimeSpan timeout, Task cancelTask)
        {
            return SendCommand(cmd, fireAndForget, Task.WhenAny(Task.Delay(timeout), cancelTask));
        }

        public async Task SendCommand(string cmd, bool fireAndForget, Task cancelTask = null)
        {
            if (!Client.Connected) return;

            Task cmdTask = SendQueue.Enqueue(OwnTcpMessage.FromCommand(cmd, fireAndForget));

            if (cancelTask != null)
            {
                await Task.WhenAny(cmdTask, cancelTask).ConfigureAwait(false);
                if (!cmdTask.IsCompleted) throw new TimeoutException($"Command ran in timout: {cmd}");
            }
            else await cmdTask.ConfigureAwait(false);
        }

        public Task CloseAsync()
        {
            return CloseAsync(null, true);
        }

        public async Task CloseAsync(Exception e, bool awaitAll)
        {
            if (!Client.Connected) return;

            if (e == null) await SendCommand(OwnTcpCommunicator.CloseCmd, true).ConfigureAwait(false);

            PingSem.Release();
            SendQueue.End();

            await ProcessQueue.End().ConfigureAwait(false);

            Client.Dispose();

            foreach (OwnTcpSendMessage message in Waits.Values)
            {
                message.SetResult(null);
            }

            Task raiseTask = RaiseDisconnected();
            if (awaitAll) await raiseTask.ConfigureAwait(false);

            async Task RaiseDisconnected()
            {
                if (Task != null) await Task.ConfigureAwait(false);

                Disconnected?.Invoke(this, new DisconnectedEventArgs(e == null, e));
            }
        }

        public void Dispose()
        {
            DisposeTask().Wait();

            async Task DisposeTask()
            {
                await CloseAsync(null, false).ConfigureAwait(false);
            }
        }
    }
}
