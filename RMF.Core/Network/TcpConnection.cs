using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public sealed class TcpConnection : INetworkConnection, IDisposable
    {
        private readonly TcpClient _client;
        public IPEndPoint RemoteEndPoint { get; }
        public int SendBufferSize { get; }
        public int ReceiveBufferSize { get; }

        public TcpConnection(TcpClient client)
        {
            this._client = client;
            this.RemoteEndPoint = (IPEndPoint)this._client.Client.RemoteEndPoint!;
            this.SendBufferSize = this._client.SendBufferSize;
            this.ReceiveBufferSize = this._client.ReceiveBufferSize;
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
