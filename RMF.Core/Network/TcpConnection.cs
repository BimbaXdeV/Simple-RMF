using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public class TcpConnection : INetworkConnection
    {
        private readonly TcpClient _client;

        public IPEndPoint RemoteEndPoint => (IPEndPoint)this._client.Client.RemoteEndPoint!;
        public int SendBufferSize => this._client.SendBufferSize;
        public int ReceiveBufferSize => this._client.ReceiveBufferSize;

        public TcpConnection(TcpClient client)
        {
            this._client = client;
        }

        public Stream GetNetworkStream()
        {
            return this._client.GetStream();
        }

        public void Close()
        {
            this._client.Close();
        }

        public void Dispose()
        {
            this._client.Dispose();
        }
    }
}
