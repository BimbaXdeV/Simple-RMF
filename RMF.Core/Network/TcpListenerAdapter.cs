using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public sealed class TcpListenerAdapter : IConnectionListener
    {
        private readonly TcpListener _listener;

        public IPEndPoint ListenedEndPoint => (IPEndPoint)this._listener.LocalEndpoint;

        public TcpListenerAdapter(TcpListener listener)
        {
            this._listener = listener;
        }

        public void Start()
        {
            this._listener.Start();
        }

        public void Stop()
        {
            this._listener.Stop();
        }

        public async Task<INetworkConnection> AcceptConnectionAsync(CancellationToken token)
        {
            TcpClient tcpClient = await this._listener.AcceptTcpClientAsync(token);
            return new TcpConnection(tcpClient);
        }
    }
}
