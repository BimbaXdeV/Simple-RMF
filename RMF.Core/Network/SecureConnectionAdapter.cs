using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public class SecureConnectionAdapter : INetworkConnection
    {
        private readonly INetworkConnection _baseConnection;
        private readonly SslStream _sslStream;

        public SecureConnectionAdapter(INetworkConnection baseConnection, SslStream sslStream)
        {
            this._baseConnection = baseConnection;
            this._sslStream = sslStream;
        }

        public IPEndPoint RemoteEndPoint => this._baseConnection.RemoteEndPoint;
        public int SendBufferSize => this._baseConnection.SendBufferSize;
        public int ReceiveBufferSize => this._baseConnection.ReceiveBufferSize;

        public Stream GetNetworkStream()
        {
            return this._sslStream;
        }

        public void Close()
        {
            this._sslStream.Close();
            this._baseConnection.Close();
        }

        public void Dispose()
        {
            this._sslStream.Dispose();
            this._baseConnection.Dispose();
        }
    }
}
