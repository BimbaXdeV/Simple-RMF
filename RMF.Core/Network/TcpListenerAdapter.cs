using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public class TcpListenerAdapter : IConnectionListener
    {
        private readonly TcpListener _listener;

        public TcpListenerAdapter(TcpListener listener)
        {
            _listener = listener;
        }

        public void Start()
        {
            _listener.Start();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public async Task<INetworkConnection> AcceptConnectionAsync(CancellationToken token)
        {
            TcpClient tcpClient = await _listener.AcceptTcpClientAsync(token);
            return new TcpConnection(tcpClient);
        }
    }
}
